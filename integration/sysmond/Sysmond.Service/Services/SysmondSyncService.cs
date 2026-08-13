using Core.DTOs.Products;
using Integration.Sysmond.Core.DTOs;
using Core.Entities;
using Core.Enums;
using Integration.Sysmond.Core.Mappings;
using Core.Mappings;
using Core.Repositories;
using Integration.Sysmond.Core.Services;
using Core.UnitOfWork;
using Core.Validations;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Integration.Sysmond.Service.Services;

/// <summary>
/// Manuel Sysmond ürün + inventory + irsaliye senkronu.
/// accessToken: istemcinin Authorization Bearer'ından gelen Sysmondax token.
/// Company: Sysmond companyId → yerel Company.Id.
/// Product orphan: ExternalSysmondId dolu ama remote stock-query'de yoksa silinir.
/// Inventory: miktar kaynağı stock/balance.rem; depolar warehouse listesi + ExternalSysmondId.
/// Inventory orphan: Sysmond-linked (ExternalSysmondId veya Product+Warehouse ExternalSysmondId)
/// ve remote (stockId, warehouseId) set'te yoksa silinir.
/// Despatch: PurchaseOrder + PurchaseOrderItem (belge); stok hareketi ayrıca StockTransaction.
/// Despatch orphan: aktif CompanyPeriod içindeki Sysmond-linked PO satırları.
/// </summary>
public partial class SysmondSyncService : ISysmondSyncService
{
    private readonly ISysmondStockQueryService _stockQuery;
    private readonly ISysmondInventoryQueryService _inventoryQuery;
    private readonly ISysmondDespatchQueryService _despatchQuery;
    private readonly ISysmondActQueryService _actQuery;
    private readonly ISysmondStockReceiptQueryService _stockReceiptQuery;
    private readonly ISysmondStockCommandService _stockCommand;
    private readonly ISysmondDespatchCommandService _despatchCommand;
    private readonly IProductRepository _productRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IWarehouseRepository _warehouseRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;
    private readonly IActRepository _actRepository;
    private readonly IActAddressRepository _actAddressRepository;
    private readonly IStockTransactionRepository _stockTransactionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SysmondSyncService> _logger;

    public SysmondSyncService(
        ISysmondStockQueryService stockQuery,
        ISysmondInventoryQueryService inventoryQuery,
        ISysmondDespatchQueryService despatchQuery,
        ISysmondActQueryService actQuery,
        ISysmondStockReceiptQueryService stockReceiptQuery,
        ISysmondStockCommandService stockCommand,
        ISysmondDespatchCommandService despatchCommand,
        IProductRepository productRepository,
        ICompanyRepository companyRepository,
        IWarehouseRepository warehouseRepository,
        IInventoryRepository inventoryRepository,
        IPurchaseOrderRepository purchaseOrderRepository,
        IActRepository actRepository,
        IActAddressRepository actAddressRepository,
        IStockTransactionRepository stockTransactionRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ILogger<SysmondSyncService> logger)
    {
        _stockQuery = stockQuery;
        _inventoryQuery = inventoryQuery;
        _despatchQuery = despatchQuery;
        _actQuery = actQuery;
        _stockReceiptQuery = stockReceiptQuery;
        _stockCommand = stockCommand;
        _despatchCommand = despatchCommand;
        _productRepository = productRepository;
        _companyRepository = companyRepository;
        _warehouseRepository = warehouseRepository;
        _inventoryRepository = inventoryRepository;
        _purchaseOrderRepository = purchaseOrderRepository;
        _actRepository = actRepository;
        _actAddressRepository = actAddressRepository;
        _stockTransactionRepository = stockTransactionRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<SysmondProductSyncResult> SyncProductsAsync(
        Guid sysmondCompanyId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);

        var stocks = await _stockQuery.GetAllStocksAsync(accessToken, sysmondCompanyId, cancellationToken);
        var errors = new List<string>();
        var result = new SysmondProductSyncResult { Fetched = stocks.Count };

        var companyCache = new Dictionary<Guid, Company?>();

        foreach (var stock in stocks)
        {
            try
            {
                if (!companyCache.TryGetValue(stock.CompanyId, out var company))
                {
                    company = await _companyRepository.GetByIdAsync(stock.CompanyId, cancellationToken);
                    companyCache[stock.CompanyId] = company;
                }

                if (company is null)
                {
                    result.SkippedCompanyNotFound++;
                    result.Failed++;
                    errors.Add($"Company bulunamadı (Sysmond CompanyId={stock.CompanyId}, StockId={stock.Id}).");
                    continue;
                }

                var existing = await _productRepository.GetByExternalSysmondIdAsync(stock.Id, cancellationToken);
                if (existing is null)
                {
                    var sku = string.IsNullOrWhiteSpace(stock.Code) ? stock.Id.ToString("N") : stock.Code.Trim();
                    existing = await _productRepository.GetBySkuAsync(company.Id, sku, cancellationToken);
                }

                Product product;
                if (existing is null)
                {
                    product = SysmondProductMapper.ToNewProduct(stock, company.Id);
                    await _productRepository.AddAsync(product, cancellationToken);
                    result.Created++;
                }
                else
                {
                    if (existing.CompanyId != company.Id)
                    {
                        result.Failed++;
                        errors.Add(
                            $"SKU/ExternalSysmondId başka şirkette (StockId={stock.Id}, LocalCompany={existing.CompanyId}, SysmondCompany={company.Id}).");
                        continue;
                    }

                    SysmondProductMapper.ApplyToProduct(existing, stock);
                    _productRepository.Update(existing);
                    product = existing;
                    result.Updated++;
                }

                await ApplyOpeningsAsync(product, stock, company.Id, result, cancellationToken);
            }
            catch (Exception ex)
            {
                result.Failed++;
                var msg = $"StockId={stock.Id}: {ex.Message}";
                errors.Add(msg);
                _logger.LogWarning(ex, "Sysmond ürün senkron satırı başarısız: {StockId}", stock.Id);
            }
        }

        await DeleteProductsMissingFromRemoteAsync(sysmondCompanyId, stocks, result, errors, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        result.Errors = errors;
        return result;
    }

    /// <inheritdoc />
    public async Task<SysmondInventorySyncResult> SyncInventoriesAsync(
        Guid sysmondCompanyId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);

        var errors = new List<string>();
        var result = new SysmondInventorySyncResult();

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken);
        if (company is null)
        {
            result.SkippedCompanyNotFound++;
            result.Failed++;
            result.Errors = [$"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId})."];
            return result;
        }

        IReadOnlyList<SysmondWarehouseDto> remoteWarehouses;
        try
        {
            remoteWarehouses = await _inventoryQuery.GetWarehousesAsync(
                accessToken, sysmondCompanyId, cancellationToken);
        }
        catch (Exception ex)
        {
            result.Failed++;
            result.Errors = [$"Warehouse listesi alınamadı: {ex.Message}"];
            _logger.LogWarning(ex, "Sysmond warehouse listesi başarısız: {CompanyId}", sysmondCompanyId);
            return result;
        }

        result.WarehousesFetched = remoteWarehouses.Count;

        Guid companyPeriodId;
        try
        {
            var periods = await _inventoryQuery.GetMyCompanyPeriodsAsync(accessToken, cancellationToken);
            var period =
                periods.FirstOrDefault(p => p.CompanyId == company.Id && p.IsActive)
                ?? periods.FirstOrDefault(p => p.CompanyId == company.Id);

            if (period is null || period.Id == Guid.Empty)
            {
                result.Failed++;
                result.Errors =
                [
                    $"CompanyPeriod bulunamadı (CompanyId={company.Id}). my-company-periods boş veya şirket eşleşmedi."
                ];
                return result;
            }

            companyPeriodId = period.Id;
            _logger.LogInformation(
                "Sysmond inventory sync CompanyPeriod seçildi: {CompanyPeriodId} ({PeriodName})",
                companyPeriodId,
                period.Name);
        }
        catch (Exception ex)
        {
            result.Failed++;
            result.Errors = [$"my-company-periods alınamadı: {ex.Message}"];
            _logger.LogWarning(ex, "Sysmond my-company-periods başarısız: {CompanyId}", sysmondCompanyId);
            return result;
        }

        // warehouseExternalId → local warehouse (upsert sonrası)
        var warehouseByExternal = new Dictionary<Guid, Warehouse>();
        foreach (var remoteWh in remoteWarehouses)
        {
            try
            {
                if (remoteWh.CompanyId != Guid.Empty && remoteWh.CompanyId != company.Id)
                {
                    result.Failed++;
                    errors.Add(
                        $"Warehouse başka şirkette (WarehouseId={remoteWh.Id}, CompanyId={remoteWh.CompanyId}).");
                    continue;
                }

                var existing = await _warehouseRepository.GetByExternalSysmondIdAsync(
                    remoteWh.Id, cancellationToken);

                // Geçiş: Local.Id == Sysmond.Id ve ExternalSysmondId henüz yoksa claim et.
                if (existing is null)
                {
                    var byId = await _warehouseRepository.GetByIdAsync(remoteWh.Id, cancellationToken);
                    if (byId is not null && byId.CompanyId == company.Id && byId.ExternalSysmondId is null)
                        existing = byId;
                }

                if (existing is null)
                {
                    var created = SysmondInventoryMapper.ToNewWarehouse(remoteWh, company.Id);
                    await _warehouseRepository.AddAsync(created, cancellationToken);
                    warehouseByExternal[remoteWh.Id] = created;
                    result.WarehousesCreated++;
                }
                else
                {
                    if (existing.CompanyId != company.Id)
                    {
                        result.Failed++;
                        errors.Add(
                            $"Warehouse ExternalSysmondId başka şirkette (WarehouseId={remoteWh.Id}, LocalCompany={existing.CompanyId}).");
                        continue;
                    }

                    SysmondInventoryMapper.ApplyToWarehouse(existing, remoteWh);
                    _warehouseRepository.Update(existing);
                    warehouseByExternal[remoteWh.Id] = existing;
                    result.WarehousesUpdated++;
                }
            }
            catch (Exception ex)
            {
                result.Failed++;
                errors.Add($"WarehouseId={remoteWh.Id}: {ex.Message}");
                _logger.LogWarning(ex, "Sysmond warehouse senkron satırı başarısız: {WarehouseId}", remoteWh.Id);
            }
        }

        // warehouse-stock → ExternalSysmondId (opsiyonel; 403 soft-fail)
        var warehouseStockIdByKey = new Dictionary<(Guid StockId, Guid WarehouseId), Guid>();
        try
        {
            var warehouseStocks = await _inventoryQuery.GetWarehouseStocksAsync(
                accessToken, sysmondCompanyId, cancellationToken);
            foreach (var ws in warehouseStocks)
            {
                if (ws.Id == Guid.Empty || ws.StockId == Guid.Empty || ws.WarehouseId == Guid.Empty)
                    continue;
                warehouseStockIdByKey[(ws.StockId, ws.WarehouseId)] = ws.Id;
            }
        }
        catch (Exception ex)
        {
            errors.Add($"warehouse-stock atlandı: {ex.Message}");
            _logger.LogWarning(
                ex,
                "Sysmond warehouse-stock başarısız (devam); CompanyId={CompanyId}",
                sysmondCompanyId);
        }

        var balances = new List<SysmondStockBalanceDto>();
        var localProducts = await _productRepository.GetByCompanyIdAsync(company.Id, cancellationToken);
        var remoteStockIds = localProducts
            .Where(p => p.ExternalSysmondId is Guid)
            .Select(p => p.ExternalSysmondId!.Value)
            .Distinct()
            .ToList();

        foreach (var stockId in remoteStockIds)
        {
            try
            {
                // WarehouseId ile değil StockId ile: sandbox'ta depo kırılımı böyle geliyor.
                var page = await _inventoryQuery.GetStockBalancesByStockAsync(
                    accessToken, companyPeriodId, stockId, cancellationToken);
                balances.AddRange(page);
            }
            catch (Exception ex)
            {
                result.Failed++;
                errors.Add($"stock/balance StockId={stockId}: {ex.Message}");
                _logger.LogWarning(
                    ex,
                    "Sysmond stock/balance başarısız: CompanyPeriodId={CompanyPeriodId}, StockId={StockId}",
                    companyPeriodId,
                    stockId);
            }
        }

        result.Fetched = balances.Count;
        var remoteKeys = new HashSet<(Guid StockId, Guid WarehouseId)>();

        foreach (var balance in balances)
        {
            try
            {
                if (balance.StockId == Guid.Empty || balance.WarehouseId == Guid.Empty)
                {
                    result.Failed++;
                    errors.Add("stock/balance satırında StockId veya WarehouseId boş.");
                    continue;
                }

                remoteKeys.Add((balance.StockId, balance.WarehouseId));

                var product = await _productRepository.GetByExternalSysmondIdAsync(
                    balance.StockId, cancellationToken);
                if (product is null || product.CompanyId != company.Id)
                {
                    result.SkippedProductNotFound++;
                    continue;
                }

                if (!warehouseByExternal.TryGetValue(balance.WarehouseId, out var warehouse))
                {
                    warehouse = await _warehouseRepository.GetByExternalSysmondIdAsync(
                        balance.WarehouseId, cancellationToken);
                    if (warehouse is not null)
                        warehouseByExternal[balance.WarehouseId] = warehouse;
                }

                if (warehouse is null || warehouse.CompanyId != company.Id)
                {
                    result.SkippedWarehouseNotFound++;
                    continue;
                }

                Guid? warehouseStockExternalId = null;
                if (warehouseStockIdByKey.TryGetValue((balance.StockId, balance.WarehouseId), out var wsId))
                    warehouseStockExternalId = wsId;

                Inventory? inventory = null;
                if (warehouseStockExternalId is Guid extId)
                    inventory = await _inventoryRepository.GetByExternalSysmondIdAsync(extId, cancellationToken);

                inventory ??= await _inventoryRepository.GetByProductAndWarehouseAsync(
                    product.Id, warehouse.Id, cancellationToken);

                var quantity = SysmondInventoryMapper.MapQuantity(balance.Rem);

                if (inventory is null)
                {
                    inventory = new Inventory
                    {
                        Id = Guid.NewGuid(),
                        CompanyId = company.Id,
                        ProductId = product.Id,
                        WarehouseId = warehouse.Id,
                        ExternalSysmondId = warehouseStockExternalId,
                        Quantity = quantity,
                        LastUpdated = DateTime.UtcNow
                    };
                    await _inventoryRepository.AddAsync(inventory, cancellationToken);
                    result.Created++;
                }
                else
                {
                    if (inventory.CompanyId != company.Id)
                    {
                        result.Failed++;
                        errors.Add(
                            $"Inventory başka şirkette (Product={product.Id}, Warehouse={warehouse.Id}).");
                        continue;
                    }

                    inventory.Quantity = quantity;
                    inventory.LastUpdated = DateTime.UtcNow;
                    if (warehouseStockExternalId is Guid wsExt)
                        inventory.ExternalSysmondId = wsExt;
                    _inventoryRepository.Update(inventory);
                    result.Updated++;
                }
            }
            catch (Exception ex)
            {
                result.Failed++;
                errors.Add($"StockId={balance.StockId}, WarehouseId={balance.WarehouseId}: {ex.Message}");
                _logger.LogWarning(
                    ex,
                    "Sysmond inventory senkron satırı başarısız: StockId={StockId}, WarehouseId={WarehouseId}",
                    balance.StockId,
                    balance.WarehouseId);
            }
        }

        await DeleteInventoriesMissingFromRemoteAsync(
            company.Id, remoteKeys, result, errors, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        result.Errors = errors;
        return result;
    }

    /// <inheritdoc />
    public async Task<SysmondFullSyncResult> SyncAllAsync(
        Guid sysmondCompanyId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);

        var products = await SyncProductsAsync(sysmondCompanyId, accessToken, cancellationToken);
        var inventories = await SyncInventoriesAsync(sysmondCompanyId, accessToken, cancellationToken);
        return new SysmondFullSyncResult
        {
            Products = products,
            Inventories = inventories
        };
    }


    /// <inheritdoc />
    public async Task<ProductResponse> CreateStockAsync(
        Guid sysmondCompanyId,
        string accessToken,
        SysmondCreateStockRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Name) && string.IsNullOrWhiteSpace(request.Code))
        {
            throw new ValidationException(
            [
                new ValidationFailure("name", "name veya code zorunludur.")
            ]);
        }

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId}).");

        var body = SysmondProductMapper.ToStockCreateDto(request, company.Id);
        if (body.Price is null)
        {
            throw new ValidationException(
            [
                new ValidationFailure("price", "price zorunludur (sale/purchase + currency + measureUnitId).")
            ]);
        }

        if (body.MeasureUnitId is null || body.MeasureUnitId == Guid.Empty)
        {
            throw new ValidationException(
            [
                new ValidationFailure("measureUnitId", "measureUnitId zorunludur.")
            ]);
        }

        if (!string.IsNullOrWhiteSpace(body.Code))
        {
            var existingSku = await _productRepository.GetBySkuAsync(company.Id, body.Code, cancellationToken);
            if (existingSku is not null)
                throw new InvalidOperationException($"Bu şirkette SKU zaten kullanılıyor: {body.Code}");
        }

        var sysmondStockId = await _stockCommand.CreateStockAsync(accessToken, body, cancellationToken);

        var product = SysmondProductMapper.ToNewProductFromCreate(request, company.Id, sysmondStockId);
        await _productRepository.AddAsync(product, cancellationToken);

        var openings = request.OpeningQuantity ?? [];
        foreach (var opening in openings)
        {
            if (opening.WarehouseId is null || opening.WarehouseId == Guid.Empty)
                continue;

            var qty = opening.Quantity.HasValue
                ? (int)Math.Round(opening.Quantity.Value, MidpointRounding.AwayFromZero)
                : 0;
            if (qty < 0)
                qty = 0;

            var warehouse = await _warehouseRepository.GetByExternalSysmondIdAsync(
                opening.WarehouseId.Value, cancellationToken);
            warehouse ??= await _warehouseRepository.GetByIdAsync(opening.WarehouseId.Value, cancellationToken);

            if (warehouse is null || warehouse.CompanyId != company.Id)
            {
                _logger.LogWarning(
                    "Sysmond stock create: opening warehouse bulunamadı WarehouseId={WarehouseId}",
                    opening.WarehouseId);
                continue;
            }

            var inventory = await _inventoryRepository.GetByProductAndWarehouseAsync(
                product.Id, warehouse.Id, cancellationToken);

            if (inventory is null)
            {
                await _inventoryRepository.AddAsync(new Inventory
                {
                    Id = Guid.NewGuid(),
                    CompanyId = company.Id,
                    ProductId = product.Id,
                    WarehouseId = warehouse.Id,
                    Quantity = qty,
                    LastUpdated = DateTime.UtcNow
                }, cancellationToken);
            }
            else
            {
                inventory.Quantity = qty;
                inventory.LastUpdated = DateTime.UtcNow;
                _inventoryRepository.Update(inventory);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductMapper.ToResponse(product);
    }

    /// <inheritdoc />
    public async Task<ProductResponse> UpdateStockAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondStockId,
        SysmondUpdateStockRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);
        ArgumentNullException.ThrowIfNull(request);

        if (sysmondStockId == Guid.Empty)
        {
            throw new ValidationException(
            [
                new ValidationFailure("id", "Sysmond stock id zorunludur.")
            ]);
        }

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId}).");

        var product = await _productRepository.GetByExternalSysmondIdAsync(sysmondStockId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Product bulunamadı (ExternalSysmondId={sysmondStockId}). Önce sync veya create yapın.");

        if (product.CompanyId != company.Id)
            throw new InvalidOperationException("Product farklı şirkete ait.");

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            var existingSku = await _productRepository.GetBySkuAsync(company.Id, request.Code.Trim(), cancellationToken);
            if (existingSku is not null && existingSku.Id != product.Id)
                throw new InvalidOperationException($"Bu şirkette SKU zaten kullanılıyor: {request.Code}");
        }

        var body = SysmondProductMapper.ToStockUpdateDto(request, company.Id, sysmondStockId);
        if (body.MeasureUnitId is null || body.MeasureUnitId == Guid.Empty)
            body.MeasureUnitId = product.MeasureUnitId;

        await _stockCommand.UpdateStockAsync(accessToken, body, cancellationToken);

        SysmondProductMapper.ApplyUpdateFromRequest(product, request);
        _productRepository.Update(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ProductMapper.ToResponse(product);
    }

    /// <inheritdoc />
    public async Task DeleteStockAsync(
        Guid sysmondCompanyId,
        string accessToken,
        Guid sysmondStockId,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);

        if (sysmondStockId == Guid.Empty)
        {
            throw new ValidationException(
            [
                new ValidationFailure("id", "Sysmond stock id zorunludur.")
            ]);
        }

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId}).");

        var product = await _productRepository.GetByExternalSysmondIdAsync(sysmondStockId, cancellationToken);
        if (product is not null && product.CompanyId != company.Id)
            throw new InvalidOperationException("Product farklı şirkete ait.");

        await _stockCommand.DeleteStockAsync(accessToken, sysmondStockId, cancellationToken);

        if (product is null)
        {
            _logger.LogInformation(
                "Sysmond stock silindi; yerel Product yok ExternalSysmondId={ExternalSysmondId}",
                sysmondStockId);
            return;
        }

        var inventories = await _inventoryRepository.GetByProductIdAsync(product.Id, cancellationToken);
        foreach (var inventory in inventories)
            _inventoryRepository.Remove(inventory);

        _productRepository.Remove(product);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static void EnsureSyncArgs(Guid sysmondCompanyId, string accessToken)
    {
        if (sysmondCompanyId == Guid.Empty)
        {
            throw new ValidationException(
            [
                new ValidationFailure("companyId", "companyId zorunludur.")
            ]);
        }

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new ValidationException(
            [
                new ValidationFailure("Authorization", "Bearer Sysmondax access_token zorunludur.")
            ]);
        }
    }

    private async Task DeleteProductsMissingFromRemoteAsync(
        Guid companyId,
        IReadOnlyList<SysmondStockDto> stocks,
        SysmondProductSyncResult result,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var remoteIds = stocks.Select(s => s.Id).ToHashSet();
        var localProducts = await _productRepository.GetByCompanyIdAsync(companyId, cancellationToken);

        var orphans = localProducts
            .Where(p => p.ExternalSysmondId is Guid extId && !remoteIds.Contains(extId))
            .ToList();

        foreach (var orphan in orphans)
        {
            try
            {
                var entity = await _productRepository.GetByIdAsync(orphan.Id, cancellationToken);
                if (entity is null)
                    continue;

                var inventories = await _inventoryRepository.GetByProductIdAsync(entity.Id, cancellationToken);
                foreach (var inventory in inventories)
                    _inventoryRepository.Remove(inventory);

                _productRepository.Remove(entity);
                result.Deleted++;
            }
            catch (Exception ex)
            {
                result.FailedDeletes++;
                var msg =
                    $"Delete ExternalSysmondId={orphan.ExternalSysmondId}, ProductId={orphan.Id}: {ex.Message}";
                errors.Add(msg);
                _logger.LogWarning(
                    ex,
                    "Sysmond senkron orphan ürün silinemedi: {ProductId}, ExternalSysmondId={ExternalSysmondId}",
                    orphan.Id,
                    orphan.ExternalSysmondId);
            }
        }
    }

    /// <summary>
    /// Sysmond-linked inventory: ExternalSysmondId dolu VEYA Product+Warehouse ExternalSysmondId dolu.
    /// Remote (stockExternalId, warehouseExternalId) set'te yoksa hard delete.
    /// Local-only (mapping yok) satırlar silinmez.
    /// </summary>
    private async Task DeleteInventoriesMissingFromRemoteAsync(
        Guid companyId,
        HashSet<(Guid StockId, Guid WarehouseId)> remoteKeys,
        SysmondInventorySyncResult result,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var localInventories = await _inventoryRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        var products = await _productRepository.GetByCompanyIdAsync(companyId, cancellationToken);
        var warehouses = await _warehouseRepository.GetByCompanyIdAsync(companyId, cancellationToken);

        var productExtById = products
            .Where(p => p.ExternalSysmondId is not null)
            .ToDictionary(p => p.Id, p => p.ExternalSysmondId!.Value);
        var warehouseExtById = warehouses
            .Where(w => w.ExternalSysmondId is not null)
            .ToDictionary(w => w.Id, w => w.ExternalSysmondId!.Value);

        foreach (var local in localInventories)
        {
            try
            {
                var hasProductExt = productExtById.TryGetValue(local.ProductId, out var stockExt);
                var hasWarehouseExt = warehouseExtById.TryGetValue(local.WarehouseId, out var whExt);
                var isSysmondLinked = local.ExternalSysmondId is not null || (hasProductExt && hasWarehouseExt);

                if (!isSysmondLinked)
                    continue;

                if (hasProductExt && hasWarehouseExt && remoteKeys.Contains((stockExt, whExt)))
                    continue;

                var entity = await _inventoryRepository.GetByIdAsync(local.Id, cancellationToken);
                if (entity is null)
                    continue;

                _inventoryRepository.Remove(entity);
                result.Deleted++;
            }
            catch (Exception ex)
            {
                result.FailedDeletes++;
                errors.Add($"Delete InventoryId={local.Id}: {ex.Message}");
                _logger.LogWarning(ex, "Sysmond senkron orphan inventory silinemedi: {InventoryId}", local.Id);
            }
        }
    }

    private async Task ApplyOpeningsAsync(
        Product product,
        SysmondStockDto stock,
        Guid companyId,
        SysmondProductSyncResult result,
        CancellationToken cancellationToken)
    {
        var openings = SysmondProductMapper.MapOpeningQuantities(stock);
        if (openings.Count == 0)
            return;

        foreach (var (remoteWarehouseId, quantity) in openings)
        {
            var warehouse = await _warehouseRepository.GetByExternalSysmondIdAsync(
                remoteWarehouseId, cancellationToken);

            // Geriye dönük: Local.Id == Sysmond warehouseId
            warehouse ??= await _warehouseRepository.GetByIdAsync(remoteWarehouseId, cancellationToken);

            if (warehouse is null || warehouse.CompanyId != companyId)
            {
                result.OpeningsSkipped++;
                continue;
            }

            var inventory = await _inventoryRepository.GetByProductAndWarehouseAsync(
                product.Id, warehouse.Id, cancellationToken);

            if (inventory is null)
            {
                inventory = new Inventory
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    ProductId = product.Id,
                    WarehouseId = warehouse.Id,
                    Quantity = quantity,
                    LastUpdated = DateTime.UtcNow
                };
                await _inventoryRepository.AddAsync(inventory, cancellationToken);
            }
            else
            {
                inventory.Quantity = quantity;
                inventory.LastUpdated = DateTime.UtcNow;
                _inventoryRepository.Update(inventory);
            }

            result.OpeningsApplied++;
        }
    }
}

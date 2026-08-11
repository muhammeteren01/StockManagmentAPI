using Core.DTOs.Sysmond;
using Core.Entities;
using Core.Enums;
using Core.Mappings;
using Microsoft.Extensions.Logging;

namespace Service.Services.Sysmond;

public partial class SysmondSyncService
{
    private const string PlaceholderProductSku = "__SYSMOND_NO_STOCK__";

    /// <inheritdoc />
    public async Task<SysmondDespatchSyncResult> SyncDespatchesAsync(
        Guid sysmondCompanyId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        EnsureSyncArgs(sysmondCompanyId, accessToken);

        var errors = new List<string>();
        var result = new SysmondDespatchSyncResult();

        var company = await _companyRepository.GetByIdAsync(sysmondCompanyId, cancellationToken);
        if (company is null)
        {
            result.SkippedCompanyNotFound++;
            result.Failed++;
            result.Errors = [$"Company bulunamadı (Sysmond CompanyId={sysmondCompanyId})."];
            return result;
        }

        var users = await _userRepository.GetByCompanyIdAsync(company.Id, cancellationToken);
        var syncUser = users.FirstOrDefault();
        if (syncUser is null)
        {
            result.SkippedNoUser++;
            result.Failed++;
            result.Errors =
            [
                $"Şirkette User yok; belge UserId zorunlu (CompanyId={company.Id})."
            ];
            return result;
        }

        Guid? companyPeriodId = null;
        try
        {
            var periods = await _inventoryQuery.GetMyCompanyPeriodsAsync(accessToken, cancellationToken);
            var period =
                periods.FirstOrDefault(p => p.CompanyId == company.Id && p.IsActive)
                ?? periods.FirstOrDefault(p => p.CompanyId == company.Id);

            if (period is not null && period.Id != Guid.Empty)
            {
                companyPeriodId = period.Id;
                _logger.LogInformation(
                    "Sysmond despatch sync aktif CompanyPeriod: {CompanyPeriodId} ({PeriodName})",
                    companyPeriodId,
                    period.Name);
            }
            else
            {
                errors.Add(
                    $"Aktif CompanyPeriod bulunamadı; dönem filtresi ve orphan silme atlanacak (CompanyId={company.Id}).");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"my-company-periods atlandı; orphan silme kapalı: {ex.Message}");
            _logger.LogWarning(ex, "Sysmond my-company-periods (despatch) başarısız: {CompanyId}", sysmondCompanyId);
        }

        IReadOnlyList<SysmondDespatchDto> despatches;
        try
        {
            despatches = await _despatchQuery.GetDespatchesAsync(
                accessToken, company.Id, companyPeriodId, cancellationToken);
        }
        catch (Exception ex)
        {
            result.Failed++;
            result.Errors = [$"despatch-query başarısız: {ex.Message}", .. errors];
            _logger.LogWarning(ex, "Sysmond despatch-query başarısız: {CompanyId}", sysmondCompanyId);
            return result;
        }

        result.DespatchesFetched = despatches.Count;
        var remoteDespatchIds = new HashSet<Guid>();
        var placeholderProduct = await GetOrCreatePlaceholderProductAsync(company.Id, cancellationToken);
        var companyAddressCache = new Dictionary<Guid, SysmondCompanyAddressDto?>();

        foreach (var header in despatches)
        {
            try
            {
                _ = SysmondDespatchPurchaseOrderMapper.MapDocumentType(header.Direction);
            }
            catch (Exception ex)
            {
                result.Failed++;
                errors.Add($"DespatchId={header.Id}, DocNo={header.DocNo ?? "-"}: {ex.Message}");
                continue;
            }

            remoteDespatchIds.Add(header.Id);

            IReadOnlyList<SysmondDespatchItemDto> items;
            try
            {
                items = await _despatchQuery.GetDespatchItemsAsync(accessToken, header.Id, cancellationToken);
            }
            catch (Exception ex)
            {
                result.Failed++;
                errors.Add($"DespatchId={header.Id} items: {ex.Message}");
                _logger.LogWarning(ex, "Sysmond despatch-items başarısız: {DespatchId}", header.Id);
                continue;
            }

            result.ItemsFetched += items.Count;

            try
            {
                var order = await UpsertDespatchOrderAsync(
                    header, company.Id, syncUser.Id, result, errors, cancellationToken);
                if (order is null)
                    continue;

                await ApplyDespatchAddressAsync(
                    order, header, accessToken, companyAddressCache, result, errors, cancellationToken);

                await SyncDespatchOrderItemsAsync(
                    order,
                    items,
                    company.Id,
                    placeholderProduct,
                    result,
                    errors,
                    cancellationToken);

                SysmondDespatchPurchaseOrderMapper.RecalcTotal(order);
            }
            catch (Exception ex)
            {
                result.Failed++;
                errors.Add($"DespatchId={header.Id}: {ex.Message}");
                _logger.LogWarning(ex, "Sysmond despatch belge senkronu başarısız: {DespatchId}", header.Id);
            }
        }

        if (companyPeriodId is Guid syncPeriodId)
        {
            await DeleteDespatchOrdersMissingFromRemoteAsync(
                company.Id, syncPeriodId, remoteDespatchIds, result, errors, cancellationToken);
        }
        else
        {
            _logger.LogInformation(
                "Sysmond despatch sync orphan delete atlandı (CompanyPeriod yok). RemoteDespatchIds={Count}",
                remoteDespatchIds.Count);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        result.Errors = errors;
        return result;
    }

    private async Task<PurchaseOrder?> UpsertDespatchOrderAsync(
        SysmondDespatchDto header,
        Guid companyId,
        Guid userId,
        SysmondDespatchSyncResult result,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var existing = await _purchaseOrderRepository.GetByExternalSysmondIdWithItemsAsync(
            header.Id, cancellationToken);

        if (existing is null)
        {
            var created = SysmondDespatchPurchaseOrderMapper.ToNewOrder(header, companyId, userId);
            await _purchaseOrderRepository.AddAsync(created, cancellationToken);
            result.Created++;
            return created;
        }

        if (existing.CompanyId != companyId)
        {
            result.Failed++;
            errors.Add(
                $"PurchaseOrder ExternalSysmondId başka şirkette (DespatchId={header.Id}, LocalCompany={existing.CompanyId}).");
            return null;
        }

        SysmondDespatchPurchaseOrderMapper.ApplyHeader(existing, header);
        _purchaseOrderRepository.Update(existing);
        result.Updated++;
        return existing;
    }

    private async Task ApplyDespatchAddressAsync(
        PurchaseOrder order,
        SysmondDespatchDto header,
        string accessToken,
        Dictionary<Guid, SysmondCompanyAddressDto?> companyAddressCache,
        SysmondDespatchSyncResult result,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        SysmondDespatchPurchaseOrderMapper.ApplyCompanyAddressId(order, header);

        // 1) Teslimat adresi (yoksa Sysmond 403 → null, failed sayılmaz)
        try
        {
            var delivery = await _despatchQuery.GetDespatchDeliveryAddressAsync(
                accessToken, header.Id, cancellationToken);
            if (delivery is not null)
            {
                SysmondDespatchPurchaseOrderMapper.ApplyAddressJson(order, delivery);
                return;
            }
        }
        catch (Exception ex)
        {
            result.Failed++;
            errors.Add(
                $"DocNo={order.OrderNumber}, DespatchId={header.Id}: delivery-address: {ex.Message}");
            _logger.LogWarning(
                ex,
                "Sysmond despatch delivery-address senkronu başarısız: {DespatchId}",
                header.Id);
        }

        // 2) Cari taraf adresi (despatch-party — UI’daki “firma adresi”)
        try
        {
            var parties = await _despatchQuery.GetDespatchPartiesAsync(
                accessToken, order.CompanyId, header.Id, cancellationToken);
            var party = SysmondDespatchPurchaseOrderMapper.PickPartyAddress(parties, header.Direction);
            if (party is not null)
            {
                SysmondDespatchPurchaseOrderMapper.ApplyAddressJson(order, party);
                return;
            }
        }
        catch (Exception ex)
        {
            result.Failed++;
            errors.Add(
                $"DocNo={order.OrderNumber}, DespatchId={header.Id}: despatch-party: {ex.Message}");
            _logger.LogWarning(
                ex,
                "Sysmond despatch-party senkronu başarısız: {DespatchId}",
                header.Id);
        }

        // 3) Son çare: kendi şirket adresi (companyAddressId)
        if (header.CompanyAddressId is Guid companyAddressId && companyAddressId != Guid.Empty)
        {
            try
            {
                if (!companyAddressCache.TryGetValue(companyAddressId, out var companyAddress))
                {
                    companyAddress = await _despatchQuery.GetCompanyAddressByIdAsync(
                        accessToken, companyAddressId, cancellationToken);
                    companyAddressCache[companyAddressId] = companyAddress;
                }

                SysmondDespatchPurchaseOrderMapper.ApplyAddressJson(order, companyAddress);
                if (companyAddress is null)
                {
                    errors.Add(
                        $"DocNo={order.OrderNumber}, DespatchId={header.Id}: şirket adresi bulunamadı (CompanyAddressId={companyAddressId}).");
                }

                return;
            }
            catch (Exception ex)
            {
                result.Failed++;
                errors.Add(
                    $"DocNo={order.OrderNumber}, DespatchId={header.Id}: company-address: {ex.Message}");
                _logger.LogWarning(
                    ex,
                    "Sysmond company-address senkronu başarısız: {CompanyAddressId}",
                    companyAddressId);
                return;
            }
        }

        SysmondDespatchPurchaseOrderMapper.ApplyAddressJson(order, null);
    }

    private async Task SyncDespatchOrderItemsAsync(
        PurchaseOrder order,
        IReadOnlyList<SysmondDespatchItemDto> items,
        Guid companyId,
        Product placeholderProduct,
        SysmondDespatchSyncResult result,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var remoteItemIds = new HashSet<Guid>();

        foreach (var item in items)
        {
            if (item.Id == Guid.Empty)
            {
                result.SkippedInvalidItem++;
                errors.Add($"DocNo={order.OrderNumber}, DespatchId={order.ExternalSysmondId}: item.id boş.");
                continue;
            }

            remoteItemIds.Add(item.Id);

            Product product;
            var missingStock = item.StockId is null || item.StockId == Guid.Empty;
            if (missingStock)
            {
                product = placeholderProduct;
            }
            else
            {
                var found = await _productRepository.GetByExternalSysmondIdAsync(
                    item.StockId!.Value, cancellationToken);
                if (found is null || found.CompanyId != companyId)
                {
                    result.SkippedProductNotFound++;
                    errors.Add(
                        $"DocNo={order.OrderNumber}, ItemId={item.Id}: Product bulunamadı (StockId={item.StockId}).");
                    continue;
                }

                product = found;
            }

            Guid? warehouseId = null;
            if (item.WarehouseId is Guid whExt && whExt != Guid.Empty)
            {
                var warehouse = await _warehouseRepository.GetByExternalSysmondIdAsync(whExt, cancellationToken);
                if (warehouse is null || warehouse.CompanyId != companyId)
                {
                    errors.Add(
                        $"DocNo={order.OrderNumber}, ItemId={item.Id}: Warehouse bulunamadı, WarehouseId null yazıldı (WarehouseId={item.WarehouseId}).");
                }
                else
                {
                    warehouseId = warehouse.Id;
                }
            }

            var existingItem = order.Items.FirstOrDefault(i => i.ExternalSysmondId == item.Id);
            if (existingItem is null)
            {
                var created = SysmondDespatchPurchaseOrderMapper.ToNewItem(
                    item, order.Id, product.Id, warehouseId);
                order.Items.Add(created);
                // Header Created sayacı zaten arttı; kalem create ayrı sayılmaz — Updated/Created belge bazlı.
            }
            else
            {
                SysmondDespatchPurchaseOrderMapper.ApplyItem(existingItem, item, product.Id, warehouseId);
            }
        }

        var orphanItems = order.Items
            .Where(i => i.ExternalSysmondId is Guid ext && !remoteItemIds.Contains(ext))
            .ToList();
        foreach (var orphan in orphanItems)
            order.Items.Remove(orphan);
    }

    private async Task DeleteDespatchOrdersMissingFromRemoteAsync(
        Guid companyId,
        Guid companyPeriodId,
        HashSet<Guid> remoteDespatchIds,
        SysmondDespatchSyncResult result,
        List<string> errors,
        CancellationToken cancellationToken)
    {
        var local = await _purchaseOrderRepository.GetSysmondDespatchesByCompanyPeriodAsync(
            companyId, companyPeriodId, cancellationToken);

        var orphans = local
            .Where(o => o.ExternalSysmondId is Guid ext && !remoteDespatchIds.Contains(ext))
            .ToList();

        foreach (var orphan in orphans)
        {
            try
            {
                _purchaseOrderRepository.Remove(orphan);
                result.Deleted++;
            }
            catch (Exception ex)
            {
                result.FailedDeletes++;
                errors.Add(
                    $"Delete ExternalSysmondId={orphan.ExternalSysmondId}, OrderId={orphan.Id}: {ex.Message}");
                _logger.LogWarning(
                    ex,
                    "Sysmond senkron orphan irsaliye belgesi silinemedi: {OrderId}, ExternalSysmondId={ExternalSysmondId}",
                    orphan.Id,
                    orphan.ExternalSysmondId);
            }
        }
    }

    private async Task<Product> GetOrCreatePlaceholderProductAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var existing = await _productRepository.GetBySkuAsync(companyId, PlaceholderProductSku, cancellationToken);
        if (existing is not null)
            return existing;

        var product = new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Sku = PlaceholderProductSku,
            Name = "Sysmond stok yok",
            Description = "Sysmond irsaliye kaleminde StockId yok / atanmamış.",
            Type = ProductType.Goods,
            Status = ProductStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _productRepository.AddAsync(product, cancellationToken);
        return product;
    }
}

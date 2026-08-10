using Core.DTOs.Sysmond;
using Core.Entities;
using Core.Enums;

namespace Core.Mappings;

/// <summary>
/// Sysmond StockDto → Product (ve açılış miktarı taslakları) eşlemesi.
/// CategoryId / SupplierId set edilmez; Type (10/20) kullanılır.
/// </summary>
public static class SysmondProductMapper
{
    /// <summary>Yeni Product oluşturur (CompanyId dışarıdan resolve edilmiş olmalı).</summary>
    public static Product ToNewProduct(SysmondStockDto stock, Guid localCompanyId)
    {
        var (purchasePrice, purchaseCurrencyId, salePrice, saleCurrencyId) = ResolvePrices(stock);

        return new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = localCompanyId,
            CategoryId = null,
            SupplierId = null,
            Type = MapType(stock.Type),
            ExternalSysmondId = stock.Id,
            MeasureUnitId = stock.MeasureUnitId,
            Sku = string.IsNullOrWhiteSpace(stock.Code) ? stock.Id.ToString("N") : stock.Code.Trim(),
            Name = string.IsNullOrWhiteSpace(stock.Name) ? stock.Code ?? stock.Id.ToString("N") : stock.Name.Trim(),
            Description = stock.Description?.Trim() ?? string.Empty,
            UnitPrice = purchasePrice,
            PurchaseCurrencyId = purchaseCurrencyId,
            SellingPrice = salePrice,
            SaleCurrencyId = saleCurrencyId,
            MinStockLevel = 0,
            Status = stock.IsActive ? ProductStatus.Active : ProductStatus.Discontinued,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>Mevcut Product üzerine Sysmond alanlarını uygular (CompanyId / Id korunur).</summary>
    public static void ApplyToProduct(Product entity, SysmondStockDto stock)
    {
        var (purchasePrice, purchaseCurrencyId, salePrice, saleCurrencyId) = ResolvePrices(stock);

        entity.ExternalSysmondId = stock.Id;
        entity.Type = MapType(stock.Type);
        entity.MeasureUnitId = stock.MeasureUnitId;
        entity.Sku = string.IsNullOrWhiteSpace(stock.Code) ? entity.Sku : stock.Code.Trim();
        entity.Name = string.IsNullOrWhiteSpace(stock.Name) ? entity.Name : stock.Name.Trim();
        entity.Description = stock.Description?.Trim() ?? string.Empty;
        entity.UnitPrice = purchasePrice;
        entity.PurchaseCurrencyId = purchaseCurrencyId;
        entity.SellingPrice = salePrice;
        entity.SaleCurrencyId = saleCurrencyId;
        entity.Status = stock.IsActive ? ProductStatus.Active : ProductStatus.Discontinued;
        // CategoryId / SupplierId bilinçli olarak dokunulmaz.
    }

    /// <summary>
    /// openingQuantity → Inventory Quantity taslağı.
    /// Sync: ExternalSysmondId veya Local.Id == warehouseId ile depo resolve eder.
    /// </summary>
    public static IReadOnlyList<(Guid WarehouseId, int Quantity)> MapOpeningQuantities(SysmondStockDto stock)
    {
        if (stock.OpeningQuantity is null || stock.OpeningQuantity.Count == 0)
            return Array.Empty<(Guid, int)>();

        var list = new List<(Guid, int)>();
        foreach (var item in stock.OpeningQuantity)
        {
            if (item.WarehouseId is null || item.WarehouseId == Guid.Empty)
                continue;

            var qty = item.Quantity.HasValue
                ? (int)Math.Round(item.Quantity.Value, MidpointRounding.AwayFromZero)
                : 0;

            if (qty < 0)
                qty = 0;

            list.Add((item.WarehouseId.Value, qty));
        }

        return list;
    }

    public static ProductType MapType(int sysmondType) =>
        sysmondType == (int)ProductType.Services ? ProductType.Services : ProductType.Goods;

    /// <summary>
    /// Query yanıtında fiyatlar prices[] olarak gelir (StockPriceDto).
    /// Create DTO'daki saleCurrencyId / purchaseCurrencyId burada yok;
    /// stockPriceTypeName / IsDefault ile best-effort ayrılır.
    /// </summary>
    public static (
        decimal PurchasePrice,
        int? PurchaseCurrencyId,
        decimal SalePrice,
        int? SaleCurrencyId) ResolvePrices(SysmondStockDto stock)
    {
        var prices = stock.Prices;
        if (prices is null || prices.Count == 0)
            return (0m, null, 0m, null);

        SysmondStockPriceDto? sale = null;
        SysmondStockPriceDto? purchase = null;

        foreach (var p in prices)
        {
            var name = p.StockPriceTypeName ?? string.Empty;
            if (IsSaleTypeName(name))
                sale ??= p;
            else if (IsPurchaseTypeName(name))
                purchase ??= p;
        }

        sale ??= prices.FirstOrDefault(p => p.IsDefault) ?? prices[0];
        purchase ??= prices.FirstOrDefault(p => !ReferenceEquals(p, sale)) ?? sale;

        return (
            ToDecimal(purchase.UnitPrice),
            purchase.CurrencyId == 0 ? null : purchase.CurrencyId,
            ToDecimal(sale.UnitPrice),
            sale.CurrencyId == 0 ? null : sale.CurrencyId);
    }

    private static bool IsSaleTypeName(string name) =>
        name.Contains("sale", StringComparison.OrdinalIgnoreCase)
        || name.Contains("satış", StringComparison.OrdinalIgnoreCase)
        || name.Contains("satis", StringComparison.OrdinalIgnoreCase);

    private static bool IsPurchaseTypeName(string name) =>
        name.Contains("purchase", StringComparison.OrdinalIgnoreCase)
        || name.Contains("alış", StringComparison.OrdinalIgnoreCase)
        || name.Contains("alis", StringComparison.OrdinalIgnoreCase)
        || name.Contains("alım", StringComparison.OrdinalIgnoreCase)
        || name.Contains("alim", StringComparison.OrdinalIgnoreCase);

    private static decimal ToDecimal(double value) =>
        Math.Round((decimal)value, 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Yerel create isteği → Sysmond StockCreateDto.
    /// measureUnitId / currency yoksa çağıran varsayılan vermeli.
    /// </summary>
    public static SysmondStockCreateDto ToStockCreateDto(
        SysmondCreateStockRequest request,
        Guid companyId)
    {
        var measureUnitId = request.MeasureUnitId
            ?? request.Price?.MeasureUnitId
            ?? Guid.Empty;

        var price = request.Price;
        if (price is not null && price.MeasureUnitId == Guid.Empty && measureUnitId != Guid.Empty)
        {
            price = new SysmondDefaultStockPriceCreateDto
            {
                SaleCurrencyId = price.SaleCurrencyId,
                SaleUnitPrice = price.SaleUnitPrice,
                PurchaseCurrencyId = price.PurchaseCurrencyId,
                PurchaseUnitPrice = price.PurchaseUnitPrice,
                Description = price.Description,
                MeasureUnitId = measureUnitId
            };
        }

        return new SysmondStockCreateDto
        {
            Name = request.Name?.Trim(),
            Description = request.Description?.Trim(),
            BrandName = request.BrandName?.Trim(),
            ModelName = request.ModelName?.Trim(),
            CompanyId = companyId,
            Type = request.Type == 20 ? 20 : 10,
            VatPercent = request.VatPercent,
            IsActive = request.IsActive,
            Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim(),
            MeasureUnitId = measureUnitId == Guid.Empty ? null : measureUnitId,
            StockTrackingEnabled = request.StockTrackingEnabled,
            StockQuantityControlEnabled = request.StockQuantityControlEnabled,
            Price = price,
            OpeningQuantity = request.OpeningQuantity
        };
    }

    /// <summary>Sysmond create + dönen id → yerel Product.</summary>
    public static Product ToNewProductFromCreate(
        SysmondCreateStockRequest request,
        Guid localCompanyId,
        Guid sysmondStockId)
    {
        var sale = request.Price?.SaleUnitPrice ?? 0;
        var purchase = request.Price?.PurchaseUnitPrice ?? sale;
        var saleCur = request.Price?.SaleCurrencyId;
        var purchaseCur = request.Price?.PurchaseCurrencyId;

        return new Product
        {
            Id = Guid.NewGuid(),
            CompanyId = localCompanyId,
            CategoryId = null,
            SupplierId = null,
            Type = MapType(request.Type),
            ExternalSysmondId = sysmondStockId,
            MeasureUnitId = request.MeasureUnitId ?? request.Price?.MeasureUnitId,
            Sku = string.IsNullOrWhiteSpace(request.Code)
                ? sysmondStockId.ToString("N")
                : request.Code.Trim(),
            Name = string.IsNullOrWhiteSpace(request.Name)
                ? request.Code ?? sysmondStockId.ToString("N")
                : request.Name.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            UnitPrice = ToDecimal(purchase),
            PurchaseCurrencyId = purchaseCur is 0 or null ? null : purchaseCur,
            SellingPrice = ToDecimal(sale),
            SaleCurrencyId = saleCur is 0 or null ? null : saleCur,
            MinStockLevel = 0,
            Status = request.IsActive ? ProductStatus.Active : ProductStatus.Discontinued,
            CreatedAt = DateTime.UtcNow
        };
    }
}

using Core.DTOs.Acts;
using Core.DTOs.Products;
using Core.DTOs.Warehouses;
using Core.Enums;
using Integration.Sysmond.Core.DTOs;

namespace Integration.Sysmond.Core.Mappings;

/// <summary>Domain Product/Warehouse/Act DTO → Sysmond command DTO.</summary>
public static class DomainToSysmondMapper
{
    public static SysmondCreateStockRequest ToCreateStockRequest(CreateProductRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.MeasureUnitId is null || request.MeasureUnitId == Guid.Empty)
            throw new InvalidOperationException("Sysmond stok oluşturmak için MeasureUnitId zorunludur.");

        return new SysmondCreateStockRequest
        {
            Name = request.Name,
            Description = request.Description,
            Code = request.Sku,
            Type = request.Type == ProductType.Services ? 20 : 10,
            IsActive = request.Status == ProductStatus.Active,
            MeasureUnitId = request.MeasureUnitId,
            CompanyId = request.CompanyId,
            Price = new SysmondDefaultStockPriceCreateDto
            {
                SaleUnitPrice = (double)request.SellingPrice,
                PurchaseUnitPrice = (double)request.UnitPrice,
                SaleCurrencyId = request.SaleCurrencyId ?? 949,
                PurchaseCurrencyId = request.PurchaseCurrencyId ?? 949,
                MeasureUnitId = request.MeasureUnitId.Value
            }
        };
    }

    public static SysmondUpdateStockRequest ToUpdateStockRequest(UpdateProductRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new SysmondUpdateStockRequest
        {
            Name = request.Name,
            Description = request.Description,
            Code = request.Sku,
            Type = request.Type == ProductType.Services ? 20 : 10,
            IsActive = request.Status == ProductStatus.Active,
            MeasureUnitId = request.MeasureUnitId
        };
    }

    public static SysmondCreateWarehouseRequest ToCreateWarehouseRequest(CreateWarehouseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new SysmondCreateWarehouseRequest
        {
            Name = request.Name,
            WarehouseCode = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location
        };
    }

    public static SysmondUpdateWarehouseRequest ToUpdateWarehouseRequest(UpdateWarehouseRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new SysmondUpdateWarehouseRequest
        {
            Name = request.Name,
            WarehouseCode = request.Location
        };
    }

    public static SysmondCreateActRequest ToCreateActRequest(CreateActRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new SysmondCreateActRequest
        {
            Type = request.Type <= 0 ? 20 : request.Type,
            Name = request.Name,
            Surname = request.Surname,
            Title = request.Title,
            ActCode = request.ActCode,
            VknTckn = request.VknTckn,
            CountryId = request.CountryId ?? 1,
            MainCurrencyId = request.MainCurrencyId
        };
    }

    public static SysmondUpdateActRequest ToUpdateActRequest(UpdateActRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new SysmondUpdateActRequest
        {
            Type = request.Type,
            Name = request.Name,
            Surname = request.Surname,
            Title = request.Title,
            ActCode = request.ActCode,
            VknTckn = request.VknTckn,
            TaxOfficeName = request.TaxOfficeName,
            ActFullAddress = request.ActFullAddress,
            CountryId = request.CountryId,
            CityId = request.CityId,
            CityOther = request.CityOther,
            Scenario = request.Scenario,
            IsDisabled = request.IsDisabled,
            IsAbroadCustomer = request.IsAbroadCustomer
        };
    }

    public static ActResponse ToActResponse(SysmondActResponse remote, Guid companyId) => new()
    {
        Id = remote.Id,
        CompanyId = companyId,
        ExternalSysmondId = remote.ExternalSysmondId,
        Type = remote.Type,
        Name = remote.Name,
        Surname = remote.Surname,
        Title = remote.Title,
        ActCode = remote.ActCode,
        VknTckn = remote.VknTckn,
        TaxOfficeName = remote.TaxOfficeName,
        ActFullAddress = remote.ActFullAddress,
        CountryId = remote.CountryId,
        CityId = remote.CityId,
        CityOther = remote.CityOther,
        Scenario = remote.Scenario,
        IsDisabled = remote.IsDisabled,
        IsAbroadCustomer = remote.IsAbroadCustomer,
        SyncedAt = remote.SyncedAt
    };
}

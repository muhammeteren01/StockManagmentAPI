using Integration.Sysmond.Core.DTOs;
using Core.Entities;

namespace Integration.Sysmond.Core.Mappings;

/// <summary>Sysmond warehouse / balance → yerel Warehouse / Inventory.</summary>
public static class SysmondInventoryMapper
{
    public static Warehouse ToNewWarehouse(SysmondWarehouseDto remote, Guid localCompanyId) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyId = localCompanyId,
            ExternalSysmondId = remote.Id,
            Name = string.IsNullOrWhiteSpace(remote.Name)
                ? remote.WarehouseCode ?? remote.Id.ToString("N")
                : remote.Name.Trim(),
            Location = remote.WarehouseCode?.Trim() ?? string.Empty,
            Capacity = null,
            IsActive = remote.IsActive ?? true
        };

    public static void ApplyToWarehouse(Warehouse entity, SysmondWarehouseDto remote)
    {
        entity.ExternalSysmondId = remote.Id;
        if (!string.IsNullOrWhiteSpace(remote.Name))
            entity.Name = remote.Name.Trim();
        if (!string.IsNullOrWhiteSpace(remote.WarehouseCode))
            entity.Location = remote.WarehouseCode.Trim();
        if (remote.IsActive.HasValue)
            entity.IsActive = remote.IsActive.Value;
    }

    /// <summary>Create isteği → Sysmond WarehouseCreateDto.</summary>
    public static SysmondWarehouseCreateDto ToWarehouseCreateDto(
        SysmondCreateWarehouseRequest request,
        Guid companyId) =>
        new()
        {
            CompanyId = companyId,
            Name = request.Name?.Trim(),
            WarehouseCode = request.WarehouseCode?.Trim()
        };

    /// <summary>Create sonrası yerel Warehouse entity.</summary>
    public static Warehouse ToNewWarehouseFromCreate(
        SysmondCreateWarehouseRequest request,
        Guid localCompanyId,
        Guid sysmondWarehouseId) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyId = localCompanyId,
            ExternalSysmondId = sysmondWarehouseId,
            Name = string.IsNullOrWhiteSpace(request.Name)
                ? request.WarehouseCode?.Trim() ?? sysmondWarehouseId.ToString("N")
                : request.Name.Trim(),
            Location = request.WarehouseCode?.Trim() ?? string.Empty,
            Capacity = null,
            IsActive = true
        };

    /// <summary>Update isteği → Sysmond WarehouseUpdateDto.</summary>
    public static SysmondWarehouseUpdateDto ToWarehouseUpdateDto(
        Warehouse entity,
        SysmondUpdateWarehouseRequest request,
        Guid sysmondCompanyId,
        Guid sysmondWarehouseId) =>
        new()
        {
            Id = sysmondWarehouseId,
            CompanyId = sysmondCompanyId,
            Name = !string.IsNullOrWhiteSpace(request.Name)
                ? request.Name.Trim()
                : entity.Name,
            WarehouseCode = request.WarehouseCode is not null
                ? request.WarehouseCode.Trim()
                : entity.Location
        };

    /// <summary>Update isteğini yerel Warehouse'a uygular.</summary>
    public static void ApplyUpdateFromRequest(Warehouse entity, SysmondUpdateWarehouseRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Name))
            entity.Name = request.Name.Trim();
        if (request.WarehouseCode is not null)
            entity.Location = request.WarehouseCode.Trim();
    }

    /// <summary>Stock balance <c>rem</c> → Inventory.Quantity (negatif clamp 0).</summary>
    public static int MapQuantity(double rem)
    {
        var qty = (int)Math.Round(rem, MidpointRounding.AwayFromZero);
        return qty < 0 ? 0 : qty;
    }
}

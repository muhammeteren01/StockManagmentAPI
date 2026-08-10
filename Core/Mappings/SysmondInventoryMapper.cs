using Core.DTOs.Sysmond;
using Core.Entities;

namespace Core.Mappings;

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

    /// <summary>Stock balance <c>rem</c> → Inventory.Quantity (negatif clamp 0).</summary>
    public static int MapQuantity(double rem)
    {
        var qty = (int)Math.Round(rem, MidpointRounding.AwayFromZero);
        return qty < 0 ? 0 : qty;
    }
}

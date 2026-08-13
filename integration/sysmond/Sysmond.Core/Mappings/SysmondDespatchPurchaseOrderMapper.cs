using System.Text.Json;
using System.Text.Json.Serialization;
using Integration.Sysmond.Core.DTOs;
using Core.Entities;
using Core.Enums;

namespace Integration.Sysmond.Core.Mappings;

/// <summary>Sysmond despatch header/item → PurchaseOrder / PurchaseOrderItem.</summary>
public static class SysmondDespatchPurchaseOrderMapper
{
    private static readonly JsonSerializerOptions AddressJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static PurchaseOrderDocumentType MapDocumentType(int direction) =>
        direction switch
        {
            SysmondDespatchMapper.DirectionIncoming => PurchaseOrderDocumentType.IncomingDespatch,
            SysmondDespatchMapper.DirectionOutgoing => PurchaseOrderDocumentType.OutgoingDespatch,
            _ => throw new InvalidOperationException($"Desteklenmeyen despatch direction: {direction}")
        };

    public static DespatchDirection MapDirection(int direction) =>
        direction switch
        {
            SysmondDespatchMapper.DirectionIncoming => DespatchDirection.Incoming,
            SysmondDespatchMapper.DirectionOutgoing => DespatchDirection.Outgoing,
            _ => throw new InvalidOperationException($"Desteklenmeyen despatch direction: {direction}")
        };

    public static PurchaseOrderStatus MapStatus(int status) =>
        status switch
        {
            10 => PurchaseOrderStatus.Draft,
            20 => PurchaseOrderStatus.Sent,
            21 => PurchaseOrderStatus.Saved,
            30 => PurchaseOrderStatus.WaitingConfirmation,
            40 => PurchaseOrderStatus.Accepted,
            41 => PurchaseOrderStatus.PartiallyAccepted,
            42 => PurchaseOrderStatus.AutomaticallyAccepted,
            50 => PurchaseOrderStatus.Rejected,
            60 => PurchaseOrderStatus.WaitingImport,
            -100 => PurchaseOrderStatus.Cancelled,
            _ => PurchaseOrderStatus.Draft
        };

    public static string BuildOrderNumber(SysmondDespatchDto header)
    {
        var doc = string.IsNullOrWhiteSpace(header.DocNo) ? "DESPATCH" : header.DocNo.Trim();
        var suffix = header.Id.ToString("N")[..8];
        var combined = $"{doc}-{suffix}";
        return combined.Length <= 100 ? combined : combined[..100];
    }

    public static PurchaseOrder ToNewOrder(
        SysmondDespatchDto header,
        Guid companyId,
        Guid userId)
    {
        var order = new PurchaseOrder
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            DocumentType = MapDocumentType(header.Direction),
            Direction = MapDirection(header.Direction),
            UserId = userId,
            OrderNumber = BuildOrderNumber(header),
            Status = MapStatus(header.Status),
            ExternalSysmondId = header.Id,
            ExternalSysmondCompanyPeriodId = NormalizePeriodId(header.CompanyPeriodId),
            ActName = Truncate(header.ActName, 255),
            ActVknTckn = Truncate(header.ActVknTckn, 20),
            IssueDate = header.IssueDate == default ? null : header.IssueDate,
            ActualDespatchDate = header.ActualDespatchDate == default ? null : header.ActualDespatchDate,
            TotalAmount = 0,
            CreatedAt = DateTime.UtcNow
        };
        ApplyCompanyAddressId(order, header);
        return order;
    }

    public static void ApplyHeader(PurchaseOrder entity, SysmondDespatchDto header)
    {
        entity.DocumentType = MapDocumentType(header.Direction);
        entity.Direction = MapDirection(header.Direction);
        entity.OrderNumber = BuildOrderNumber(header);
        entity.Status = MapStatus(header.Status);
        entity.ExternalSysmondId = header.Id;
        entity.ExternalSysmondCompanyPeriodId = NormalizePeriodId(header.CompanyPeriodId);
        entity.ActName = Truncate(header.ActName, 255);
        entity.ActVknTckn = Truncate(header.ActVknTckn, 20);
        entity.IssueDate = header.IssueDate == default ? entity.IssueDate : header.IssueDate;
        entity.ActualDespatchDate =
            header.ActualDespatchDate == default ? entity.ActualDespatchDate : header.ActualDespatchDate;
        ApplyCompanyAddressId(entity, header);
    }

    public static void ApplyCompanyAddressId(PurchaseOrder entity, SysmondDespatchDto header)
    {
        entity.ExternalSysmondCompanyAddressId =
            header.CompanyAddressId is Guid id && id != Guid.Empty ? id : null;
    }

    /// <summary>
    /// Adres DTO → <see cref="PurchaseOrder.DeliveryAddressJson"/> (camelCase JSON).
    /// <paramref name="address"/> null ise kolon temizlenir.
    /// Teslimat veya firma (company) adresi aynı kolona yazılır.
    /// </summary>
    public static void ApplyAddressJson(PurchaseOrder entity, object? address)
    {
        if (address is null)
        {
            entity.DeliveryAddressJson = null;
            return;
        }

        entity.DeliveryAddressJson = JsonSerializer.Serialize(address, AddressJsonOptions);
    }

    /// <inheritdoc cref="ApplyAddressJson"/>
    public static void ApplyDeliveryAddress(
        PurchaseOrder entity,
        SysmondDespatchDeliveryAddressDto? address)
        => ApplyAddressJson(entity, address);

    /// <summary>
    /// Incoming → SellerSupplier öncelik; Outgoing → Delivery/Buyer öncelik.
    /// Sokak/şehir bilgisi olan taraf seçilir.
    /// </summary>
    public static SysmondDespatchPartyDto? PickPartyAddress(
        IReadOnlyList<SysmondDespatchPartyDto> parties,
        int direction)
    {
        if (parties.Count == 0)
            return null;

        var preferredTypes = direction == SysmondDespatchMapper.DirectionIncoming
            ? new[] { 30, 20, 10, 40 } // SellerSupplier, BuyerCustomer, DeliveryCustomer, Originator
            : new[] { 10, 20, 30, 40 }; // DeliveryCustomer, BuyerCustomer, SellerSupplier, Originator

        foreach (var type in preferredTypes)
        {
            var match = parties.FirstOrDefault(p => p.Type == type && HasPartyAddress(p));
            if (match is not null)
                return match;
        }

        return parties.FirstOrDefault(HasPartyAddress) ?? parties.FirstOrDefault();
    }

    private static bool HasPartyAddress(SysmondDespatchPartyDto party) =>
        !string.IsNullOrWhiteSpace(party.Street)
        || party.CityId is > 0
        || !string.IsNullOrWhiteSpace(party.CityOther)
        || !string.IsNullOrWhiteSpace(party.DistrictOther)
        || !string.IsNullOrWhiteSpace(party.PostalZone)
        || !string.IsNullOrWhiteSpace(party.BuildingName)
        || !string.IsNullOrWhiteSpace(party.BuildingNumber);

    public static PurchaseOrderItem ToNewItem(
        SysmondDespatchItemDto item,
        Guid purchaseOrderId,
        Guid productId,
        Guid? warehouseId)
    {
        var qty = SysmondDespatchMapper.MapQuantity(item.Quantity);
        return new PurchaseOrderItem
        {
            Id = Guid.NewGuid(),
            PurchaseOrderId = purchaseOrderId,
            ProductId = productId,
            WarehouseId = warehouseId,
            ExternalSysmondId = item.Id,
            MeasureUnitId = item.MeasureUnitId,
            StockPriceId = item.StockPriceId,
            Name = Truncate(item.Name, 255),
            Code = Truncate(item.Code, 100),
            Quantity = qty,
            UnitPrice = (decimal)item.UnitPrice,
            VatPercent = item.VatPercent,
            ReceivedQuantity = 0
        };
    }

    public static void ApplyItem(
        PurchaseOrderItem entity,
        SysmondDespatchItemDto item,
        Guid productId,
        Guid? warehouseId)
    {
        entity.ProductId = productId;
        entity.WarehouseId = warehouseId;
        entity.ExternalSysmondId = item.Id;
        entity.MeasureUnitId = item.MeasureUnitId;
        entity.StockPriceId = item.StockPriceId;
        entity.Name = Truncate(item.Name, 255);
        entity.Code = Truncate(item.Code, 100);
        entity.Quantity = SysmondDespatchMapper.MapQuantity(item.Quantity);
        entity.UnitPrice = (decimal)item.UnitPrice;
        entity.VatPercent = item.VatPercent;
    }

    public static void RecalcTotal(PurchaseOrder order)
    {
        order.TotalAmount = order.Items.Sum(i => i.Quantity * i.UnitPrice);
    }

    private static Guid? NormalizePeriodId(Guid companyPeriodId)
        => companyPeriodId == Guid.Empty ? null : companyPeriodId;

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}

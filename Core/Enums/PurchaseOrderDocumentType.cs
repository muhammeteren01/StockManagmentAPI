namespace Core.Enums;

/// <summary>
/// purchase_orders satırının belge türü.
/// Aynı tabloda satın alma siparişi ile Sysmond irsaliyeleri tutulur.
/// </summary>
public enum PurchaseOrderDocumentType
{
    PurchaseOrder = 1,
    IncomingDespatch = 2,
    OutgoingDespatch = 3
}

namespace Core.Enums;

/// <summary>
/// Satın alma siparişi + irsaliye (despatch) yaşam döngüsü.
/// Klasik PO: Pending / Approved / Received / Cancelled.
/// İrsaliye: Sysmond DespatchStatuses değerleriyle uyumlu ek durumlar.
/// </summary>
public enum PurchaseOrderStatus
{
    Pending = 1,
    Approved = 2,
    Received = 3,
    Cancelled = 4,

    Draft = 10,
    Sent = 20,
    Saved = 21,
    WaitingConfirmation = 30,
    Accepted = 40,
    PartiallyAccepted = 41,
    AutomaticallyAccepted = 42,
    Rejected = 50,
    WaitingImport = 60
}

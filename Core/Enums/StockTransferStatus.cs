namespace Core.Enums;

/// <summary>Depolar arası transferin yaşam döngüsü durumu.</summary>
public enum StockTransferStatus
{
    Pending = 1,
    InTransit = 2,
    Completed = 3,
    Cancelled = 4
}

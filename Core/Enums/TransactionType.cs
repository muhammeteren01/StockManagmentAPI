namespace Core.Enums;

/// <summary>Stok hareketinin türü; quantity yönünü (+/-) bu değer belirler.</summary>
public enum TransactionType
{
    In = 1,
    Out = 2,
    TransferOut = 3,
    TransferIn = 4,
    Adjustment = 5
}

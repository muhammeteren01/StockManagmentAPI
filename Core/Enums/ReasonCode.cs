namespace Core.Enums;

/// <summary>Stok düzeltme/çıkış nedeni (opsiyonel; çoğunlukla Adjustment ve Out işlemlerinde kullanılır).</summary>
public enum ReasonCode
{
    Damage = 1,
    Lost = 2,
    CountDiff = 3,
    Expired = 4
}

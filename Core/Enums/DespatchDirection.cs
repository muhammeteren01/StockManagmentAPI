namespace Core.Enums;

/// <summary>
/// Sysmond DespatchDirection ile uyumlu yön.
/// purchase_orders üzerinde irsaliye için kullanılır; klasik PO'da null olabilir.
/// </summary>
public enum DespatchDirection
{
    Incoming = 10,
    Outgoing = 20
}

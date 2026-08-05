namespace Core.Exceptions;

/// <summary>Çakışma durumları (örn. e-posta zaten kayıtlı).</summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}

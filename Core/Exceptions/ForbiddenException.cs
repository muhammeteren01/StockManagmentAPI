namespace Core.Exceptions;

/// <summary>Kimlik doğrulanmış kullanıcının yetkisi yetersiz (403).</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}

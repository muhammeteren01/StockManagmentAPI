namespace Core.Exceptions;

/// <summary>Yetkisiz erişim veya hatalı kimlik bilgisi durumları.</summary>
public class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}

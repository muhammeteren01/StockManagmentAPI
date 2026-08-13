namespace Integration.Sysmond.Core.Orchestration;

/// <summary>Sunucu tarafında Sysmondax access token sağlar (kullanıcı Bearer gerekmez).</summary>
public interface ISysmondAccessTokenProvider
{
    Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default);
}

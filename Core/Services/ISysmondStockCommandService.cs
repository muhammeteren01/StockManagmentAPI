using Core.DTOs.Sysmond;

namespace Core.Services;

/// <summary>Sysmondax stok create/update HTTP istemcisi.</summary>
public interface ISysmondStockCommandService
{
    /// <summary><c>POST /api/app/stock</c> — dönen Sysmond stock id.</summary>
    Task<Guid> CreateStockAsync(
        string accessToken,
        SysmondStockCreateDto body,
        CancellationToken cancellationToken = default);

    /// <summary><c>PUT /api/app/stock</c>.</summary>
    Task UpdateStockAsync(
        string accessToken,
        SysmondStockUpdateDto body,
        CancellationToken cancellationToken = default);
}

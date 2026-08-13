using Integration.Sysmond.Core.DTOs;

namespace Integration.Sysmond.Core.Services;

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

    /// <summary><c>DELETE /api/app/stock/{id}</c>.</summary>
    Task DeleteStockAsync(
        string accessToken,
        Guid sysmondStockId,
        CancellationToken cancellationToken = default);
}

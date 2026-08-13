using Integration.Sysmond.Core.DTOs;

namespace Integration.Sysmond.Core.Services;

/// <summary>Sysmondax despatch command (incoming + outgoing draft/item/save).</summary>
public interface ISysmondDespatchCommandService
{
    /// <summary><c>POST /api/app/incoming-despatch/draft</c> → despatch id.</summary>
    Task<Guid> CreateIncomingDraftAsync(
        string accessToken,
        SysmondIncomingDespatchCreateDto body,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST /api/app/incoming-despatch/item</c> → item id.</summary>
    Task<Guid> CreateIncomingItemAsync(
        string accessToken,
        SysmondDespatchItemCreateDto body,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST /api/app/incoming-despatch/save</c>.</summary>
    Task SaveIncomingAsync(
        string accessToken,
        SysmondIncomingDespatchSaveDto body,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST /api/app/outgoing-despatch/draft</c> → despatch id.</summary>
    Task<Guid> CreateOutgoingDraftAsync(
        string accessToken,
        SysmondOutgoingDespatchCreateDto body,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST /api/app/outgoing-despatch/item</c> → item id.</summary>
    Task<Guid> CreateOutgoingItemAsync(
        string accessToken,
        SysmondDespatchItemCreateDto body,
        CancellationToken cancellationToken = default);

    /// <summary><c>POST /api/app/outgoing-despatch/save</c>.</summary>
    Task SaveOutgoingAsync(
        string accessToken,
        SysmondIncomingDespatchSaveDto body,
        CancellationToken cancellationToken = default);

    /// <summary><c>PUT /api/app/incoming-despatch/draft</c>.</summary>
    Task UpdateIncomingDraftAsync(
        string accessToken,
        SysmondIncomingDespatchUpdateDto body,
        CancellationToken cancellationToken = default);

    /// <summary><c>PUT /api/app/outgoing-despatch/draft</c>.</summary>
    Task UpdateOutgoingDraftAsync(
        string accessToken,
        SysmondOutgoingDespatchUpdateDto body,
        CancellationToken cancellationToken = default);

    /// <summary><c>DELETE /api/app/incoming-despatch/{id}/draft-despatch</c>.</summary>
    Task DeleteIncomingDraftAsync(
        string accessToken,
        Guid despatchId,
        CancellationToken cancellationToken = default);

    /// <summary><c>DELETE /api/app/outgoing-despatch/{id}/draft-despatch</c>.</summary>
    Task DeleteOutgoingDraftAsync(
        string accessToken,
        Guid despatchId,
        CancellationToken cancellationToken = default);
}

using Core.DTOs.PurchaseOrders;
using Core.Enums;
using Core.Repositories;
using Core.Validations;
using FluentValidation.Results;
using Integration.Sysmond.Core.DTOs;
using Integration.Sysmond.Core.Orchestration;
using Integration.Sysmond.Core.Services;

namespace Integration.Sysmond.Service.Orchestration;

/// <summary>Despatch write → Sysmond draft + lokal PurchaseOrder.</summary>
public sealed class SysmondDespatchOrchestrator : ISysmondDespatchOrchestrator
{
    private readonly ISysmondAccessTokenProvider _tokenProvider;
    private readonly ISysmondSyncService _syncService;
    private readonly IPurchaseOrderRepository _purchaseOrderRepository;

    public SysmondDespatchOrchestrator(
        ISysmondAccessTokenProvider tokenProvider,
        ISysmondSyncService syncService,
        IPurchaseOrderRepository purchaseOrderRepository)
    {
        _tokenProvider = tokenProvider;
        _syncService = syncService;
        _purchaseOrderRepository = purchaseOrderRepository;
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderResponse> CreateIncomingAsync(
        Guid companyId,
        SysmondCreateIncomingDespatchRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCompany(companyId);
        ArgumentNullException.ThrowIfNull(request);
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        return await _syncService.CreateIncomingDespatchAsync(companyId, token, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderResponse> CreateOutgoingAsync(
        Guid companyId,
        SysmondCreateOutgoingDespatchRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureCompany(companyId);
        ArgumentNullException.ThrowIfNull(request);
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        return await _syncService.CreateOutgoingDespatchAsync(companyId, token, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderResponse> UpdateIncomingAsync(
        Guid purchaseOrderId,
        SysmondUpdateIncomingDespatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (companyId, externalId) = await ResolveDespatchIdsAsync(
            purchaseOrderId, DespatchDirection.Incoming, cancellationToken);
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        return await _syncService.UpdateIncomingDespatchAsync(
            companyId, token, externalId, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<PurchaseOrderResponse> UpdateOutgoingAsync(
        Guid purchaseOrderId,
        SysmondUpdateOutgoingDespatchRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var (companyId, externalId) = await ResolveDespatchIdsAsync(
            purchaseOrderId, DespatchDirection.Outgoing, cancellationToken);
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        return await _syncService.UpdateOutgoingDespatchAsync(
            companyId, token, externalId, request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteIncomingAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var (companyId, externalId) = await ResolveDespatchIdsAsync(
            purchaseOrderId, DespatchDirection.Incoming, cancellationToken);
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        await _syncService.DeleteIncomingDespatchAsync(companyId, token, externalId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteOutgoingAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var (companyId, externalId) = await ResolveDespatchIdsAsync(
            purchaseOrderId, DespatchDirection.Outgoing, cancellationToken);
        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        await _syncService.DeleteOutgoingDespatchAsync(companyId, token, externalId, cancellationToken);
    }

    private async Task<(Guid CompanyId, Guid ExternalId)> ResolveDespatchIdsAsync(
        Guid purchaseOrderId,
        DespatchDirection expected,
        CancellationToken cancellationToken)
    {
        var order = await _purchaseOrderRepository.GetByIdWithItemsAsync(purchaseOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"PurchaseOrder bulunamadı: {purchaseOrderId}");

        if (order.Direction != expected)
            throw new InvalidOperationException($"Belge yönü {expected} değil.");

        if (order.ExternalSysmondId is null || order.ExternalSysmondId == Guid.Empty)
            throw new InvalidOperationException("Belgenin Sysmond ExternalSysmondId'si yok.");

        return (order.CompanyId, order.ExternalSysmondId.Value);
    }

    private static void EnsureCompany(Guid companyId)
    {
        if (companyId == Guid.Empty)
            throw new ValidationException([new ValidationFailure("companyId", "companyId zorunludur.")]);
    }
}

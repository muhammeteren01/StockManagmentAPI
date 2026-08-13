using Core.DTOs.Products;
using Core.Repositories;
using Core.Validations;
using FluentValidation.Results;
using Integration.Sysmond.Core.Mappings;
using Integration.Sysmond.Core.Orchestration;
using Integration.Sysmond.Core.Services;

namespace Integration.Sysmond.Service.Orchestration;

/// <summary>Product write → Sysmond stock + lokal Product.</summary>
public sealed class SysmondProductOrchestrator : ISysmondProductOrchestrator
{
    private readonly ISysmondAccessTokenProvider _tokenProvider;
    private readonly ISysmondSyncService _syncService;
    private readonly IProductRepository _productRepository;

    public SysmondProductOrchestrator(
        ISysmondAccessTokenProvider tokenProvider,
        ISysmondSyncService syncService,
        IProductRepository productRepository)
    {
        _tokenProvider = tokenProvider;
        _syncService = syncService;
        _productRepository = productRepository;
    }

    /// <inheritdoc />
    public async Task<ProductResponse> CreateAsync(
        Guid companyId,
        CreateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (companyId == Guid.Empty)
            throw new ValidationException([new ValidationFailure("companyId", "companyId zorunludur.")]);

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        var body = DomainToSysmondMapper.ToCreateStockRequest(request);
        return await _syncService.CreateStockAsync(companyId, token, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ProductResponse> UpdateAsync(
        Guid productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product bulunamadı: {productId}");

        if (product.ExternalSysmondId is null || product.ExternalSysmondId == Guid.Empty)
            throw new InvalidOperationException(
                "Ürünün Sysmond ExternalSysmondId'si yok; önce sync veya Sysmond üzerinden create gerekir.");

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        var body = DomainToSysmondMapper.ToUpdateStockRequest(request);
        return await _syncService.UpdateStockAsync(
            product.CompanyId, token, product.ExternalSysmondId.Value, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetByIdAsync(productId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product bulunamadı: {productId}");

        if (product.ExternalSysmondId is null || product.ExternalSysmondId == Guid.Empty)
            throw new InvalidOperationException(
                "Ürünün Sysmond ExternalSysmondId'si yok; yalnızca lokal silme için Sysmond:Enabled=false kullanın.");

        var token = await _tokenProvider.GetAccessTokenAsync(cancellationToken);
        await _syncService.DeleteStockAsync(
            product.CompanyId, token, product.ExternalSysmondId.Value, cancellationToken);
    }
}

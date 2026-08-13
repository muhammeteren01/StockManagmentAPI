using Core.DTOs.Products;

namespace Integration.Sysmond.Core.Orchestration;

/// <summary>Tek istek: lokal Product + Sysmond stock yazma.</summary>
public interface ISysmondProductOrchestrator
{
    Task<ProductResponse> CreateAsync(
        Guid companyId,
        CreateProductRequest request,
        CancellationToken cancellationToken = default);

    Task<ProductResponse> UpdateAsync(
        Guid productId,
        UpdateProductRequest request,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid productId, CancellationToken cancellationToken = default);
}

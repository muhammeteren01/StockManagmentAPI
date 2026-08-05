using Core.Entities;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;

namespace Service.Services;

/// <summary>Product iş kuralları implementasyonu.</summary>
public class ProductService : GenericService<Product>, IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository repository, IUnitOfWork unitOfWork)
        : base(repository, unitOfWork)
    {
        _productRepository = repository;
    }

    public Task<IReadOnlyList<Product>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return _productRepository.GetByCompanyIdAsync(companyId, cancellationToken);
    }

    public override async Task<Product> CreateAsync(Product entity, CancellationToken cancellationToken = default)
    {
        var existingSku = await _productRepository.GetBySkuAsync(entity.CompanyId, entity.Sku, cancellationToken);
        if (existingSku is not null)
            throw new InvalidOperationException($"Bu şirkette SKU zaten kullanılıyor: {entity.Sku}");

        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        if (entity.CreatedAt == default)
            entity.CreatedAt = DateTime.UtcNow;

        return await base.CreateAsync(entity, cancellationToken);
    }
}

using Core.DTOs.Products;
using Core.Mappings;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;

namespace Service.Services;

/// <summary>Product iş kuralları implementasyonu (DTO).</summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<UpdateProductRequest> _updateValidator;

    public ProductService(
        IProductRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreateProductRequest> createValidator,
        IValidator<UpdateProductRequest> updateValidator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Id ile ürün getirir.</summary>
    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : ProductMapper.ToResponse(entity);
    }

    /// <summary>Tüm ürünleri listeler.</summary>
    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(ProductMapper.ToResponse).ToList();
    }

    /// <summary>Şirkete ait ürünleri listeler.</summary>
    public async Task<IReadOnlyList<ProductResponse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetByCompanyIdAsync(companyId, cancellationToken);
        return list.Select(ProductMapper.ToResponse).ToList();
    }

    /// <summary>Yeni ürün oluşturur; SKU şirket içinde unique olmalıdır.</summary>
    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        var existingSku = await _repository.GetBySkuAsync(request.CompanyId, request.Sku, cancellationToken);
        if (existingSku is not null)
            throw new InvalidOperationException($"Bu şirkette SKU zaten kullanılıyor: {request.Sku}");

        var entity = ProductMapper.ToEntity(request);
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductMapper.ToResponse(entity);
    }

    /// <summary>Ürünü günceller.</summary>
    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_updateValidator, request, cancellationToken);
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product bulunamadı: {id}");

        var existingSku = await _repository.GetBySkuAsync(entity.CompanyId, request.Sku, cancellationToken);
        if (existingSku is not null && existingSku.Id != id)
            throw new InvalidOperationException($"Bu şirkette SKU zaten kullanılıyor: {request.Sku}");

        ProductMapper.ApplyUpdate(entity, request);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductMapper.ToResponse(entity);
    }

    /// <summary>Ürünü siler.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product bulunamadı: {id}");
        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

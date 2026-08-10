using Core.Abstractions;
using Core.Authorization;
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
    private readonly ICategoryRepository _categoryRepository;
    private readonly ISupplierRepository _supplierRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateProductRequest> _createValidator;
    private readonly IValidator<UpdateProductRequest> _updateValidator;

    public ProductService(
        IProductRepository repository,
        ICategoryRepository categoryRepository,
        ISupplierRepository supplierRepository,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        IValidator<CreateProductRequest> createValidator,
        IValidator<UpdateProductRequest> updateValidator)
    {
        _repository = repository;
        _categoryRepository = categoryRepository;
        _supplierRepository = supplierRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<ProductResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : ProductMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<ProductResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(ProductMapper.ToResponse).ToList();
    }

    public async Task<IReadOnlyList<ProductResponse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        TenantGuard.EnsureCompanyAccess(_currentUser, companyId);
        var list = await _repository.GetByCompanyIdAsync(companyId, cancellationToken);
        return list.Select(ProductMapper.ToResponse).ToList();
    }

    public async Task<ProductResponse> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);
        var companyId = TenantGuard.ResolveCompanyId(_currentUser, request.CompanyId);
        await EnsureOptionalSameCompanyReferencesAsync(companyId, request.CategoryId, request.SupplierId, cancellationToken);

        var existingSku = await _repository.GetBySkuAsync(companyId, request.Sku, cancellationToken);
        if (existingSku is not null)
            throw new InvalidOperationException($"Bu şirkette SKU zaten kullanılıyor: {request.Sku}");

        var entity = ProductMapper.ToEntity(request, companyId);
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductMapper.ToResponse(entity);
    }

    public async Task<ProductResponse> UpdateAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_updateValidator, request, cancellationToken);
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product bulunamadı: {id}");

        await EnsureOptionalSameCompanyReferencesAsync(entity.CompanyId, request.CategoryId, request.SupplierId, cancellationToken);

        var existingSku = await _repository.GetBySkuAsync(entity.CompanyId, request.Sku, cancellationToken);
        if (existingSku is not null && existingSku.Id != id)
            throw new InvalidOperationException($"Bu şirkette SKU zaten kullanılıyor: {request.Sku}");

        ProductMapper.ApplyUpdate(entity, request);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ProductMapper.ToResponse(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Product bulunamadı: {id}");
        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>CategoryId / SupplierId verildiyse aynı şirket kontrolü; null veya Empty ise atlanır.</summary>
    private async Task EnsureOptionalSameCompanyReferencesAsync(
        Guid companyId, Guid? categoryId, Guid? supplierId, CancellationToken cancellationToken)
    {
        if (categoryId is Guid cid && cid != Guid.Empty)
        {
            var category = await _categoryRepository.GetByIdAsync(cid, cancellationToken)
                ?? throw new InvalidOperationException($"Kategori bulunamadı: {cid}");
            if (category.CompanyId != companyId)
                throw new InvalidOperationException("Kategori farklı bir şirkete ait.");
        }

        if (supplierId is Guid sid && sid != Guid.Empty)
        {
            var supplier = await _supplierRepository.GetByIdAsync(sid, cancellationToken)
                ?? throw new InvalidOperationException($"Tedarikçi bulunamadı: {sid}");
            if (supplier.CompanyId != companyId)
                throw new InvalidOperationException("Tedarikçi farklı bir şirkete ait.");
        }
    }
}

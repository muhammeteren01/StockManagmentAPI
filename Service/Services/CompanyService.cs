using Core.DTOs.Companies;
using Core.Mappings;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;

namespace Service.Services;

/// <summary>Company iş kuralları implementasyonu (DTO).</summary>
public class CompanyService : ICompanyService
{
    private readonly ICompanyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateCompanyRequest> _createValidator;
    private readonly IValidator<UpdateCompanyRequest> _updateValidator;

    public CompanyService(
        ICompanyRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<CreateCompanyRequest> createValidator,
        IValidator<UpdateCompanyRequest> updateValidator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Id ile şirketi getirir.</summary>
    public async Task<CompanyResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : CompanyMapper.ToResponse(entity);
    }

    /// <summary>Tüm şirketleri listeler.</summary>
    public async Task<IReadOnlyList<CompanyResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(CompanyMapper.ToResponse).ToList();
    }

    /// <summary>Yeni şirket oluşturur.</summary>
    public async Task<CompanyResponse> CreateAsync(CreateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);
        var entity = CompanyMapper.ToEntity(request);
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CompanyMapper.ToResponse(entity);
    }

    /// <summary>Şirketi günceller.</summary>
    public async Task<CompanyResponse> UpdateAsync(Guid id, UpdateCompanyRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_updateValidator, request, cancellationToken);
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı: {id}");
        CompanyMapper.ApplyUpdate(entity, request);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CompanyMapper.ToResponse(entity);
    }

    /// <summary>Şirketi siler.</summary>
    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"Company bulunamadı: {id}");
        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

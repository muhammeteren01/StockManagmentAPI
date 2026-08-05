using Core.Abstractions;
using Core.Authorization;
using Core.DTOs.Users;
using Core.Mappings;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;

namespace Service.Services;

/// <summary>User iş kuralları implementasyonu (DTO).</summary>
public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordService _passwordService;
    private readonly ICurrentUser _currentUser;
    private readonly IValidator<CreateUserRequest> _createValidator;
    private readonly IValidator<UpdateUserRequest> _updateValidator;

    public UserService(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IPasswordService passwordService,
        ICurrentUser currentUser,
        IValidator<CreateUserRequest> createValidator,
        IValidator<UpdateUserRequest> updateValidator)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _passwordService = passwordService;
        _currentUser = currentUser;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<UserResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken);
        return entity is null ? null : UserMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<UserResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetAllAsync(cancellationToken);
        return list.Select(UserMapper.ToResponse).ToList();
    }

    public async Task<UserResponse?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByEmailAsync(email, cancellationToken);
        return entity is null ? null : UserMapper.ToResponse(entity);
    }

    public async Task<IReadOnlyList<UserResponse>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        TenantGuard.EnsureCompanyAccess(_currentUser, companyId);
        var list = await _repository.GetByCompanyIdAsync(companyId, cancellationToken);
        return list.Select(UserMapper.ToResponse).ToList();
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_createValidator, request, cancellationToken);

        var companyId = TenantGuard.ResolveUserCompanyId(_currentUser, request.Role, request.CompanyId);

        var existing = await _repository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"Bu e-posta zaten kayıtlı: {request.Email}");

        var entity = UserMapper.ToEntity(request, _passwordService.HashPassword(request.Password), companyId);
        await _repository.AddAsync(entity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UserMapper.ToResponse(entity);
    }

    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        await ValidationHelper.EnsureValidAsync(_updateValidator, request, cancellationToken);
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"User bulunamadı: {id}");

        var companyId = TenantGuard.ResolveUserCompanyId(_currentUser, request.Role, request.CompanyId);
        UserMapper.ApplyUpdate(entity, request, companyId);
        _repository.Update(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return UserMapper.ToResponse(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"User bulunamadı: {id}");
        _repository.Remove(entity);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

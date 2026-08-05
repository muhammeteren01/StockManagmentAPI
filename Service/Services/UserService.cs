using Core.Entities;
using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;

namespace Service.Services;

/// <summary>User iş kuralları implementasyonu.</summary>
public class UserService : GenericService<User>, IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(
        IUserRepository repository,
        IUnitOfWork unitOfWork,
        IValidator<User> validator)
        : base(repository, unitOfWork, validator)
    {
        _userRepository = repository;
    }

    /// <summary>E-posta adresine göre kullanıcı getirir.</summary>
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _userRepository.GetByEmailAsync(email, cancellationToken);
    }

    /// <summary>Belirli şirkete ait kullanıcıları listeler.</summary>
    public Task<IReadOnlyList<User>> GetByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        return _userRepository.GetByCompanyIdAsync(companyId, cancellationToken);
    }

    /// <summary>Yeni kullanıcı oluşturur; e-posta unique olmalıdır.</summary>
    public override async Task<User> CreateAsync(User entity, CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByEmailAsync(entity.Email, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"Bu e-posta zaten kayıtlı: {entity.Email}");

        if (entity.Id == Guid.Empty)
            entity.Id = Guid.NewGuid();

        if (entity.CreatedAt == default)
            entity.CreatedAt = DateTime.UtcNow;

        return await base.CreateAsync(entity, cancellationToken);
    }
}

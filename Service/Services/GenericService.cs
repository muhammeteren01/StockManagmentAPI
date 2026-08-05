using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;
using FluentValidation;
using ValidationException = Core.Validations.ValidationException;

namespace Service.Services;

/// <summary>Genel CRUD işlemlerini repository ve UnitOfWork üzerinden yönetir.</summary>
public class GenericService<T> : IGenericService<T> where T : class
{
    protected readonly IGenericRepository<T> Repository;
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly IValidator<T> Validator;

    public GenericService(
        IGenericRepository<T> repository,
        IUnitOfWork unitOfWork,
        IValidator<T> validator)
    {
        Repository = repository;
        UnitOfWork = unitOfWork;
        Validator = validator;
    }

    /// <summary>Id ile kayıt getirir.</summary>
    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Repository.GetByIdAsync(id, cancellationToken);
    }

    /// <summary>Tüm kayıtları listeler.</summary>
    public virtual Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Repository.GetAllAsync(cancellationToken);
    }

    /// <summary>Yeni kayıt oluşturur; önce FluentValidation uygular.</summary>
    public virtual async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(entity, cancellationToken);
        await Repository.AddAsync(entity, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return entity;
    }

    /// <summary>Mevcut kaydı günceller; önce FluentValidation uygular.</summary>
    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await ValidateAsync(entity, cancellationToken);
        Repository.Update(entity);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Id ile kaydı siler.</summary>
    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"{typeof(T).Name} bulunamadı: {id}");

        Repository.Remove(entity);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Entity'yi validator ile doğrular; hatalıysa ValidationException fırlatır.</summary>
    protected async Task ValidateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var result = await Validator.ValidateAsync(entity, cancellationToken);
        if (!result.IsValid)
            throw new ValidationException(result.Errors);
    }
}

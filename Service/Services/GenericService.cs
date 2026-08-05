using Core.Repositories;
using Core.Services;
using Core.UnitOfWork;

namespace Service.Services;

/// <summary>Genel CRUD işlemlerini repository ve UnitOfWork üzerinden yönetir.</summary>
public class GenericService<T> : IGenericService<T> where T : class
{
    protected readonly IGenericRepository<T> Repository;
    protected readonly IUnitOfWork UnitOfWork;

    public GenericService(IGenericRepository<T> repository, IUnitOfWork unitOfWork)
    {
        Repository = repository;
        UnitOfWork = unitOfWork;
    }

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Repository.GetByIdAsync(id, cancellationToken);
    }

    public virtual Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Repository.GetAllAsync(cancellationToken);
    }

    public virtual async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
    {
        await Repository.AddAsync(entity, cancellationToken);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        Repository.Update(entity);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Repository.GetByIdAsync(id, cancellationToken)
            ?? throw new KeyNotFoundException($"{typeof(T).Name} bulunamadı: {id}");

        Repository.Remove(entity);
        await UnitOfWork.SaveChangesAsync(cancellationToken);
    }
}

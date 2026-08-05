using Core.UnitOfWork;
using Repository.Data;

namespace Repository.UnitOfWork;

/// <summary>DbContext üzerinden değişiklikleri tek transaction'da kaydeder.</summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }
}

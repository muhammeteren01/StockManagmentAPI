namespace Core.UnitOfWork;

/// <summary>Tüm repository'leri tek transaction üzerinden koordine eder.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

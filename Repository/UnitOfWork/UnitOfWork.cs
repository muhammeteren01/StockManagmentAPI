using Core.Exceptions;
using Core.UnitOfWork;
using Microsoft.EntityFrameworkCore;
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

    /// <summary>Değişiklikleri kaydeder; concurrent inventory güncellemelerinde ConflictException fırlatır.</summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "Stok kaydı başka bir işlem tarafından güncellendi. Lütfen tekrar deneyin.");
        }
    }
}

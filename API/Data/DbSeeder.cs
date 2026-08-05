using Core.Entities;
using Core.Enums;
using Core.Services;
using Core.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Repository.Data;

namespace API.Data;

/// <summary>
/// Uygulama açılışında yapılandırılmış SuperAdmin kullanıcısını oluşturur (yoksa).
/// Kilitli register sonrası bootstrap için kullanılır.
/// </summary>
public static class DbSeeder
{
    /// <summary>SeedSettings.Enabled ise ve kullanıcı yoksa SuperAdmin ekler.</summary>
    public static async Task SeedSuperAdminAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var settings = scope.ServiceProvider.GetRequiredService<IOptions<SeedSettings>>().Value;

        if (!settings.Enabled)
            return;

        if (string.IsNullOrWhiteSpace(settings.Email) || string.IsNullOrWhiteSpace(settings.Password))
            throw new InvalidOperationException(
                "SeedSettings.Enabled=true iken Email ve Password dolu olmalıdır. Development için User Secrets kullanın.");

        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

        var exists = await db.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Email == settings.Email, cancellationToken);

        if (exists)
        {
            logger.LogInformation("SuperAdmin seed atlandı; e-posta zaten var: {Email}", settings.Email);
            return;
        }

        db.Users.Add(new User
        {
            Id = Guid.NewGuid(),
            CompanyId = null,
            FirstName = settings.FirstName,
            LastName = settings.LastName,
            Email = settings.Email.Trim(),
            PasswordHash = passwordService.HashPassword(settings.Password),
            Role = UserRole.SuperAdmin,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("SuperAdmin seed tamamlandı: {Email}", settings.Email);
    }
}

using Core.Abstractions;
using Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Repository.Data;

/// <summary>Uygulama DbContext'i; entity konfigürasyonları ve tenant filtreleri.</summary>
public class AppDbContext : DbContext
{
    private readonly ICurrentUser _currentUser;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUser currentUser) : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<StockTransaction> StockTransactions => Set<StockTransaction>();
    public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>();
    public DbSet<StockTransferItem> StockTransferItems => Set<StockTransferItem>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => Set<PurchaseOrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // EF filter expression'larında dış alan yerine local capture kullanılır.
        var currentUser = _currentUser;

        modelBuilder.Entity<Company>().HasQueryFilter(e =>
            !currentUser.ApplyTenantFilter
            || (currentUser.CompanyId.HasValue && e.Id == currentUser.CompanyId.Value));

        modelBuilder.Entity<User>().HasQueryFilter(e =>
            !currentUser.ApplyTenantFilter
            || (currentUser.CompanyId.HasValue && e.CompanyId == currentUser.CompanyId.Value));

        modelBuilder.Entity<Supplier>().HasQueryFilter(e =>
            !currentUser.ApplyTenantFilter
            || (currentUser.CompanyId.HasValue && e.CompanyId == currentUser.CompanyId.Value));

        modelBuilder.Entity<Category>().HasQueryFilter(e =>
            !currentUser.ApplyTenantFilter
            || (currentUser.CompanyId.HasValue && e.CompanyId == currentUser.CompanyId.Value));

        modelBuilder.Entity<Warehouse>().HasQueryFilter(e =>
            !currentUser.ApplyTenantFilter
            || (currentUser.CompanyId.HasValue && e.CompanyId == currentUser.CompanyId.Value));

        modelBuilder.Entity<Product>().HasQueryFilter(e =>
            !currentUser.ApplyTenantFilter
            || (currentUser.CompanyId.HasValue && e.CompanyId == currentUser.CompanyId.Value));

        modelBuilder.Entity<PurchaseOrder>().HasQueryFilter(e =>
            !currentUser.ApplyTenantFilter
            || (currentUser.CompanyId.HasValue && e.CompanyId == currentUser.CompanyId.Value));

        modelBuilder.Entity<Inventory>().HasQueryFilter(e =>
            !currentUser.ApplyTenantFilter
            || (currentUser.CompanyId.HasValue && e.CompanyId == currentUser.CompanyId.Value));

        modelBuilder.Entity<StockTransaction>().HasQueryFilter(e =>
            !currentUser.ApplyTenantFilter
            || (currentUser.CompanyId.HasValue && e.CompanyId == currentUser.CompanyId.Value));

        modelBuilder.Entity<StockTransfer>().HasQueryFilter(e =>
            !currentUser.ApplyTenantFilter
            || (currentUser.CompanyId.HasValue && e.CompanyId == currentUser.CompanyId.Value));

        base.OnModelCreating(modelBuilder);
    }
}

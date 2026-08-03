using HoraLife.Store.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HoraLife.Store.Infrastructure.Data;

public sealed class StoreDbContext : DbContext
{
    public StoreDbContext(DbContextOptions<StoreDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Inventory> Inventory => Set<Inventory>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<RemedyKey> RemedyKeys => Set<RemedyKey>();
    public DbSet<ProductRemedyMapping> ProductRemedyMappings => Set<ProductRemedyMapping>();
    public DbSet<UserRecommendation> UserRecommendations => Set<UserRecommendation>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Shipment> Shipments => Set<Shipment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("store");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(StoreDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        SetAuditTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        SetAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void SetAuditTimestamps()
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                var createdAt = entry.Metadata.FindProperty("CreatedAt");
                if (createdAt is not null &&
                    entry.Property("CreatedAt").CurrentValue is DateTimeOffset value &&
                    value == default)
                {
                    entry.Property("CreatedAt").CurrentValue = now;
                }
            }

            if (entry.State is EntityState.Added or EntityState.Modified)
            {
                var updatedAt = entry.Metadata.FindProperty("UpdatedAt");
                if (updatedAt is not null)
                {
                    entry.Property("UpdatedAt").CurrentValue = now;
                }
            }
        }
    }
}

using Idempotency.Domain;
using Microsoft.EntityFrameworkCore;
namespace Idempotency.Infrastructure;

public sealed class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<Product>().Property(p => p.Name).HasMaxLength(200);
        model.Entity<Product>().Property(p => p.Price).HasPrecision(18, 2);
        model.Entity<IdempotencyRecord>().HasKey(r => r.Key);
        model.Entity<IdempotencyRecord>().Property(r => r.Key).HasMaxLength(200);
    }
}

public sealed class IdempotencyRecord
{
    public required string Key { get; set; }
    public required string RequestHash { get; set; }
    public required string Body { get; set; }
    public required string Location { get; set; }
    public int StatusCode { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

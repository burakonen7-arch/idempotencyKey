using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Idempotency.Application;
using Idempotency.Domain;
using Microsoft.EntityFrameworkCore;
namespace Idempotency.Infrastructure;

public sealed class ProductStore(ProductDbContext db) : IProductStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public Task<Product?> FindAsync(Guid id, CancellationToken ct) =>
        db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<StoredResponse> CreateAsync(string key, CreateProductRequest request, CancellationToken ct)
    {
        // Anahtar endpoint kapsamında. Çok kiracılı sistemde kullanıcı/tenant kimliği de eklenmeli.
        var scopedKey = "POST:/products:" + key;
        // Semantik olarak aynı decimal değerler (10 ve 10.00) aynı özeti üretir.
        var canonical = JsonSerializer.Serialize(new { request.Name, Price = request.Price.ToString("G29", System.Globalization.CultureInfo.InvariantCulture) }, Json);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        var lockId = BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes(scopedKey)));
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // PostgreSQL transaction kilidi: farklı API instance'larında bile aynı anahtarı sıraya alır.
        // Hash çakışması yalnızca ilgisiz istekleri sıraya alır; kayıt kimliği tam scopedKey'dir.
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockId})", ct);
        var previous = await db.IdempotencyRecords.FindAsync([scopedKey], ct);
        if (previous is not null)
        {
            if (previous.RequestHash != hash) throw new IdempotencyConflictException();
            await transaction.CommitAsync(ct);
            return new(previous.StatusCode, previous.Body, previous.Location, true);
        }
        var product = new Product { Id = Guid.NewGuid(), Name = request.Name, Price = request.Price };
        var body = JsonSerializer.Serialize(product, Json);
        var location = $"/products/{product.Id}";
        db.Products.Add(product);
        db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = scopedKey, RequestHash = hash, Body = body, Location = location,
            StatusCode = 201, CreatedAtUtc = DateTime.UtcNow
        });
        // Ürün ile yanıt aynı transaction'da: biri kaydolup diğeri kaybolamaz.
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(201, body, location, false);
    }
}

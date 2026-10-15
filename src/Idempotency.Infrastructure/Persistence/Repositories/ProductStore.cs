using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Idempotency.Application.DTOs;
using Idempotency.Application.Exceptions;
using Idempotency.Application.Interfaces;
using Idempotency.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Idempotency.Infrastructure.Persistence.Repositories;

public sealed class ProductStore(ProductDbContext db) : IProductStore
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public Task<Product?> FindAsync(Guid id, CancellationToken ct) =>
        db.Products.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, ct);

    public async Task<StoredResponse> CreateAsync(string key, CreateProductRequest request, CancellationToken ct)
    {
        //idempotencyKey ve request değerleri hashlenir
        var scopedKey = "POST:/products:" + key;
        var canonical = JsonSerializer.Serialize(new 
            { request.Name, Price = request.Price.ToString("G29", System.Globalization.CultureInfo.InvariantCulture) }, Json);
        
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
        var lockId = BinaryPrimitives.ReadInt64BigEndian(SHA256.HashData(Encoding.UTF8.GetBytes(scopedKey)));
        
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock({lockId})", ct);
        
        //db'de aynı key ile daha önceden bir işlem tutulmuş mu kontrolü
        var previous = await db.IdempotencyRecords.FindAsync([scopedKey], ct);
        if (previous is not null)
        {
            //eğer hashler farklıysa, ürünlerde farklıdır. ama key db'de daha önceden tutulduğu için
            //var olan key üzerinde, farklı hashler vardır demektir. Bu da key değeri aynı ama ürün bilgileri farklı demektir.
            //Dolayısıyla hata fırlatılır.
            if (previous.RequestHash != hash) throw new IdempotencyConflictException();
            
            //eğer hash ve key değerleri aynıysa transaction tamamlanır ve önceki işlemin code ve body değerleri kullanıcıya döndürülür.
            await transaction.CommitAsync(ct);
            return new(previous.StatusCode, previous.Body, previous.Location, true);
        }
        
        //eğer db'de key yoksa tüm işlemler yeniyse,
        var product = new Product { Id = Guid.NewGuid(), Name = request.Name, Price = request.Price };
        var body = JsonSerializer.Serialize(product, Json);
        var location = $"/products/{product.Id}";
        
        
        //aynı transaction içerisinde hem dbye ürün, hem de idempotency tablosuna ilgili key eklenir.
        db.Products.Add(product);
        db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Key = scopedKey, RequestHash = hash, Body = body, Location = location,
            StatusCode = 201, CreatedAtUtc = DateTime.UtcNow
        });
        
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new(201, body, location, false);
    }
}

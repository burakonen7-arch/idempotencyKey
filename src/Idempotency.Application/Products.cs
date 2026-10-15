using Idempotency.Domain;
namespace Idempotency.Application;

public sealed record CreateProductRequest(string Name, decimal Price);
public sealed record StoredResponse(int StatusCode, string Body, string Location, bool Replayed);
public sealed class IdempotencyConflictException : Exception;

// Uygulama katmanı EF Core veya HTTP altyapısına bağımlı değildir.
public interface IProductStore
{
    Task<StoredResponse> CreateAsync(string key, CreateProductRequest request, CancellationToken ct);
    Task<Product?> FindAsync(Guid id, CancellationToken ct);
}

public sealed class CreateProduct(IProductStore store)
{
    public Task<StoredResponse> ExecuteAsync(string key, CreateProductRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 128 || key.Any(char.IsControl))
            throw new ArgumentException("Idempotency-Key 1–128 karakter olmalı ve kontrol karakteri içermemeli.");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200 || request.Price <= 0
            || request.Price > 9999999999999999.99m || decimal.Round(request.Price, 2) != request.Price)
            throw new ArgumentException("İsim 1–200 karakter, fiyat pozitif ve en fazla iki ondalık basamak olmalı.");
        return store.CreateAsync(key, request with { Name = request.Name.Trim() }, ct);
    }
}

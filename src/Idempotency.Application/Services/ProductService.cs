using Idempotency.Application.DTOs;
using Idempotency.Application.Interfaces;
using Idempotency.Domain.Entities;

namespace Idempotency.Application.Services;

public sealed class ProductService(IProductStore store) : IProductService
{
    public Task<StoredResponse> CreateAsync(string key, CreateProductRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > 128 || key.Any(char.IsControl))
            throw new ArgumentException("Idempotency-Key 1–128 karakter olmalı ve kontrol karakteri içermemeli.");

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200 || request.Price <= 0
            || request.Price > 9999999999999999.99m || decimal.Round(request.Price, 2) != request.Price)
            throw new ArgumentException("İsim 1–200 karakter, fiyat pozitif ve en fazla iki ondalık basamak olmalı.");
        
        return store.CreateAsync(key, request with { Name = request.Name.Trim() }, ct);
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct) => store.FindAsync(id, ct);
}

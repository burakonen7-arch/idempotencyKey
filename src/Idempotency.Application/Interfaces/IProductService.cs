using Idempotency.Application.DTOs;
using Idempotency.Domain.Entities;

namespace Idempotency.Application.Interfaces;

public interface IProductService
{
    Task<StoredResponse> CreateAsync(string key, CreateProductRequest request, CancellationToken ct);
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct);
}

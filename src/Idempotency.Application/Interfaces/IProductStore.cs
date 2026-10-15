using Idempotency.Application.DTOs;
using Idempotency.Domain.Entities;

namespace Idempotency.Application.Interfaces;

public interface IProductStore
{
    Task<StoredResponse> CreateAsync(string key, CreateProductRequest request, CancellationToken ct);
    Task<Product?> FindAsync(Guid id, CancellationToken ct);
}

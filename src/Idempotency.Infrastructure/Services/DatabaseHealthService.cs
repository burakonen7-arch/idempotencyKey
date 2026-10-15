using Idempotency.Application.Interfaces;
using Idempotency.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Idempotency.Infrastructure.Services;

public sealed class DatabaseHealthService(ProductDbContext db) : IHealthService
{
    public Task<bool> IsReadyAsync(CancellationToken ct) => db.Database.CanConnectAsync(ct);
}

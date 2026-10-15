namespace Idempotency.Application.Interfaces;

public interface IHealthService
{
    Task<bool> IsReadyAsync(CancellationToken ct);
}

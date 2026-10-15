namespace Idempotency.Domain.Entities;

public sealed class IdempotencyRecord
{
    public required string Key { get; set; }
    public required string RequestHash { get; set; }
    public required string Body { get; set; }
    public required string Location { get; set; }
    public int StatusCode { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

namespace Idempotency.Application.DTOs;

public sealed record StoredResponse(int StatusCode, string Body, string Location, bool Replayed);

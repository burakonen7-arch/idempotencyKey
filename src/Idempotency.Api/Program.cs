using Idempotency.Application;
using Idempotency.Infrastructure;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("ConnectionStrings__Postgres ortam değişkenini ayarlayın.");
builder.Services.AddDbContext<ProductDbContext>(o => o.UseNpgsql(connection));
builder.Services.AddScoped<IProductStore, ProductStore>();
builder.Services.AddScoped<CreateProduct>();
builder.Services.AddProblemDetails();
var app = builder.Build();
app.UseExceptionHandler();
// Öğrenme projesi: boş veritabanını hazırlar. Şema evrimi için production'da migration kullanın.
await using (var scope = app.Services.CreateAsyncScope())
    await scope.ServiceProvider.GetRequiredService<ProductDbContext>().Database.EnsureCreatedAsync();

app.MapPost("/products", async (HttpContext http, CreateProductRequest request, CreateProduct useCase, CancellationToken ct) =>
{
    if (!http.Request.Headers.TryGetValue("Idempotency-Key", out var keys) || keys.Count != 1)
        return Results.Problem(statusCode: 400, detail: "Tek bir Idempotency-Key başlığı zorunlu.");
    try
    {
        var result = await useCase.ExecuteAsync(keys.ToString(), request, ct);
        http.Response.Headers.Location = result.Location;
        http.Response.Headers["Idempotency-Replayed"] = result.Replayed ? "true" : "false";
        return Results.Content(result.Body, "application/json", statusCode: result.StatusCode);
    }
    catch (ArgumentException ex) { return Results.Problem(statusCode: 400, detail: ex.Message); }
    catch (IdempotencyConflictException)
    {
        return Results.Problem(statusCode: 409, detail: "Bu anahtar farklı bir istek gövdesiyle kullanılmış. Yeni işlem için yeni anahtar kullanın.");
    }
});
app.MapGet("/products/{id:guid}", async (Guid id, IProductStore store, CancellationToken ct) =>
    await store.FindAsync(id, ct) is { } product ? Results.Ok(product) : Results.NotFound());
app.MapGet("/health", async (ProductDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "ready" }) : Results.StatusCode(503));
app.Run();

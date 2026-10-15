using Idempotency.Application.Interfaces;
using Idempotency.Application.Services;
using Idempotency.Infrastructure;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Postgres")
    ?? throw new InvalidOperationException("PostgreSQL bağlantısını appsettings.json içindeki ConnectionStrings:Postgres alanında veya ConnectionStrings__Postgres ortam değişkeninde tanımlayın.");

builder.Services.AddControllers();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Idempotency API",
        Version = "v1",
        Description = "Aynı Idempotency-Key ve istek gövdesiyle tekrar gönderilen ürün oluşturma isteği, ilk yanıtı döndürür. Yeni işlem için yeni anahtar kullanın."
    });
});
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddInfrastructure(connection);
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Idempotency API v1");
});
await app.Services.InitializeDatabaseAsync();
app.MapControllers();
app.Run();

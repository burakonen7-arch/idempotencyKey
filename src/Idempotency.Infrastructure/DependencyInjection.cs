using Idempotency.Application.Interfaces;
using Idempotency.Infrastructure.Persistence;
using Idempotency.Infrastructure.Persistence.Repositories;
using Idempotency.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Idempotency.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ProductDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IProductStore, ProductStore>();
        services.AddScoped<IHealthService, DatabaseHealthService>();
        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}

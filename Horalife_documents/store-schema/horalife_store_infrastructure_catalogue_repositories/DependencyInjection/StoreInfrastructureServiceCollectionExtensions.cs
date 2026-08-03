using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Infrastructure.Data;
using HoraLife.Store.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HoraLife.Store.Infrastructure.DependencyInjection;

public static class StoreInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddStoreInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<StoreDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable(
                        "__EFMigrationsHistory",
                        "store");
                });
        });

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();

        return services;
    }
}

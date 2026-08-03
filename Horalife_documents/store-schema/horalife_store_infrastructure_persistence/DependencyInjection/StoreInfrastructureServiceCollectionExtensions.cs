using HoraLife.Store.Infrastructure.Persistence;
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

        return services;
    }
}

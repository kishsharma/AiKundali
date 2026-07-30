using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Infrastructure.Data;
using HoraLife.Store.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HoraLife.Store.Infrastructure.DependencyInjection;

public static class StoreDependencyInjection
{
    public static IServiceCollection AddStoreInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {

        var connectionString =
    configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' is missing.");


        services.AddDbContext<StoreDbContext>(options =>
            options.UseNpgsql(
                connectionString));

        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<ICustomerAddressRepository, CustomerAddressRepository>();
        return services;
    }
}
//public static class StoreInfrastructureServiceCollectionExtensions
//{
//    public static IServiceCollection AddStoreInfrastructure(
//        this IServiceCollection services,
//        string connectionString)
//    {
//        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

//        services.AddDbContext<StoreDbContext>(options =>
//        {
//            options.UseNpgsql(
//                connectionString,
//                npgsqlOptions =>
//                {
//                    npgsqlOptions.MigrationsHistoryTable(
//                        "__EFMigrationsHistory",
//                        "store");
//                });
//        });

//        services.AddScoped<ICategoryRepository, CategoryRepository>();
//        services.AddScoped<IProductRepository, ProductRepository>();

//        return services;
//    }
//}

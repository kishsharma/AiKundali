using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HoraLife.Store.Application.DependencyInjection;

public static class StoreApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddStoreApplication(
        this IServiceCollection services)
    {
        services.AddScoped<IStoreCatalogService, StoreCatalogService>();

        return services;
    }
}

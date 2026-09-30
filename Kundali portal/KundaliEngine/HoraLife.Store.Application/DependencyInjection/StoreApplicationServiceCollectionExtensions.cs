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
        services.AddScoped<IStoreCartService, StoreCartService>();
        services.AddScoped<IStoreAddressService, StoreAddressService>();
        services.AddScoped<IStoreCheckoutPreviewService, StoreCheckoutPreviewService>();
        services.AddScoped<IStoreCheckoutRevalidationService, StoreCheckoutRevalidationService>();
        services.AddScoped<IStoreOrderService,StoreOrderService>();
        services.AddScoped<IStoreOrderItemService, StoreOrderItemService>();
        services.AddScoped<IStoreInventoryReservationService, StoreInventoryReservationService>();
        services.AddScoped<IStoreInventoryAdminService, StoreInventoryAdminService>();
        services.AddScoped<IStoreCartConversionService,StoreCartConversionService>();

        return services;
    }
}

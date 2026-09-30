using HoraLife.Store.Application.Interfaces;
using HoraLife.Store.Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace KundaliEngine.Api.Services.Store;

public sealed class StoreInventoryReservationExpiryHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptionsMonitor<StoreInventoryOptions> _options;
    private readonly ILogger<StoreInventoryReservationExpiryHostedService> _logger;

    public StoreInventoryReservationExpiryHostedService(
        IServiceScopeFactory scopeFactory,
        IOptionsMonitor<StoreInventoryOptions> options,
        ILogger<StoreInventoryReservationExpiryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;
            var sweep = TimeSpan.FromMinutes(
                Math.Max(1, options.ExpirySweepMinutes));
            var maxAge = TimeSpan.FromHours(
                Math.Max(1, options.ReservationExpiryHours));

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var inventory = scope.ServiceProvider
                    .GetRequiredService<IStoreInventoryReservationService>();

                var released = await inventory.ReleaseExpiredAsync(
                    maxAge,
                    stoppingToken);

                if (released > 0)
                {
                    _logger.LogInformation(
                        "Released inventory for {Count} expired pending order reservation(s).",
                        released);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Store inventory reservation expiry sweep failed.");
            }

            try
            {
                await Task.Delay(sweep, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}

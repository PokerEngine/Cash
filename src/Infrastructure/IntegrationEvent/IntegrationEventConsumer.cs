using Application.IntegrationEvent;

namespace Infrastructure.IntegrationEvent;

public sealed class IntegrationEventConsumer(
    IServiceScopeFactory scopeFactory,
    IIntegrationEventQueue queue,
    ILogger<IntegrationEventConsumer> logger,
    IntegrationEventChannel channel
) : BackgroundService
{
    private const int Delay = 500;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("IntegrationEventConsumer started for channel {Channel}", channel);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var integrationEvent = await queue.DequeueAsync(channel, cancellationToken);
                if (integrationEvent is null)
                {
                    await Task.Delay(Delay, cancellationToken);
                    continue;
                }

                using var scope = scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();
                await dispatcher.DispatchAsync(integrationEvent);
            }
            catch (OperationCanceledException)
            {
                // graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error while consuming integration event on channel {Channel}",
                    channel
                );
                await Task.Delay(Delay, cancellationToken);
            }
        }

        logger.LogInformation("IntegrationEventConsumer stopped for channel {Channel}", channel);
    }
}

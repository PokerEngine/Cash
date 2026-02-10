using Application.Connection;
using Application.Repository;
using Application.Service.HandManager;
using Domain.Entity;

namespace Application.IntegrationEvent;

public record PlayerSatDownIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
}

public class PlayerSatDownHandler(
    IConnectionRegistry connectionRegistry,
    IRepository repository,
    IHandManager handManager
) : IIntegrationEventHandler<PlayerSatDownIntegrationEvent>
{
    public async Task HandleAsync(PlayerSatDownIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );

        var events = await repository.GetEventsAsync(integrationEvent.TableUid);
        var table = Table.FromEvents(integrationEvent.TableUid, events);
        if (table.HasEnoughPlayersForHand() && !table.IsHandInProgress())
        {
            await handManager.StartHandAsync(table);
        }

        events = table.PullEvents();
        await repository.AddEventsAsync(table.Uid, events);
    }
}

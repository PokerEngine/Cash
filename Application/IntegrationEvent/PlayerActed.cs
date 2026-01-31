using Application.Connection;
using Application.Repository;
using Domain.Entity;

namespace Application.IntegrationEvent;

public record PlayerActedIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
    public required string Nickname { get; init; }
    public required string Type { get; init; }
    public required int Amount { get; init; }
}

public class PlayerActedHandler(
    IConnectionRegistry connectionRegistry,
    IRepository repository
) : IIntegrationEventHandler<PlayerActedIntegrationEvent>
{
    public async Task HandleAsync(PlayerActedIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );

        if (integrationEvent.Amount > 0)
        {
            var events = await repository.GetEventsAsync(integrationEvent.TableUid);
            var table = Table.FromEvents(integrationEvent.TableUid, events);

            table.DebitPlayerChips(integrationEvent.Nickname, integrationEvent.Amount);

            events = table.PullEvents();
            await repository.AddEventsAsync(table.Uid, events);
        }
    }
}

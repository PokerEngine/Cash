using Application.Connection;
using Application.Repository;
using Domain.Entity;

namespace Application.IntegrationEvent;

public struct BlindIsPostedIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
    public required string Nickname { get; init; }
    public required int Amount { get; init; }
}

public class BlindIsPostedHandler(
    IConnectionRegistry connectionRegistry,
    IRepository repository
) : IIntegrationEventHandler<BlindIsPostedIntegrationEvent>
{
    public async Task HandleAsync(BlindIsPostedIntegrationEvent integrationEvent)
    {
        var events = await repository.GetEventsAsync(integrationEvent.TableUid);
        var table = Table.FromEvents(integrationEvent.TableUid, events);
        // TODO: apply taking chips from player's stack

        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );
    }
}

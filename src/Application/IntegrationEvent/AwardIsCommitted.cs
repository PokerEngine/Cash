using Application.Connection;
using Application.Repository;
using Domain.Entity;

namespace Application.IntegrationEvent;

public struct AwardIsCommittedIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
    public required List<string> Nicknames { get; init; }
    public required int Amount { get; init; }
}

public class AwardIsCommittedHandler(
    IConnectionRegistry connectionRegistry,
    IRepository repository
) : IIntegrationEventHandler<AwardIsCommittedIntegrationEvent>
{
    public async Task HandleAsync(AwardIsCommittedIntegrationEvent integrationEvent)
    {
        var events = await repository.GetEventsAsync(integrationEvent.TableUid);
        var table = Table.FromEvents(integrationEvent.TableUid, events);
        // TODO: apply putting chips into players' stacks and taking the rake

        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );
    }
}

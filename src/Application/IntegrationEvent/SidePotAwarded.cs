using Application.Connection;
using Application.Repository;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.IntegrationEvent;

public record SidePotAwardedIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
    public required List<string> Winners { get; init; }
    public required int Amount { get; init; }
}

public class SidePotAwardedHandler(
    IConnectionRegistry connectionRegistry,
    IRepository repository
) : IIntegrationEventHandler<SidePotAwardedIntegrationEvent>
{
    public async Task HandleAsync(SidePotAwardedIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );

        if (integrationEvent.Amount > 0)
        {
            var events = await repository.GetEventsAsync(integrationEvent.TableUid);
            var table = Table.FromEvents(integrationEvent.TableUid, events);

            foreach (var (nickname, award) in SplitSidePot(integrationEvent))
            {
                table.CreditPlayerChips(nickname, award);
            }

            events = table.PullEvents();
            await repository.AddEventsAsync(table.Uid, events);
        }

        // TODO: calculate rake
    }

    private IEnumerable<(string, int)> SplitSidePot(SidePotAwardedIntegrationEvent integrationEvent)
    {
        var amount = integrationEvent.Amount;
        var amountPerWinner = amount / integrationEvent.Winners.Count;
        var remainder = amount % integrationEvent.Winners.Count;

        foreach (var nickname in integrationEvent.Winners.Order())
        {
            var award = amountPerWinner;
            if (remainder > 0)
            {
                award += 1;
                remainder -= 1;
            }

            yield return (nickname, award);
        }
    }
}

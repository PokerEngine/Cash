using Application.Connection;
using Application.Repository;
using Domain.Entity;

namespace Application.IntegrationEvent;

public struct HoleCardsAreDealtIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
    public required string Nickname { get; init; }
    public required string Cards { get; init; }
}

public class HoleCardsAreDealtHandler(
    IConnectionRegistry connectionRegistry,
    IRepository repository
) : IIntegrationEventHandler<HoleCardsAreDealtIntegrationEvent>
{
    public async Task HandleAsync(HoleCardsAreDealtIntegrationEvent integrationEvent)
    {
        var events = await repository.GetEventsAsync(integrationEvent.TableUid);
        var table = Table.FromEvents(integrationEvent.TableUid, events);

        var anonymousIntegrationEvent = new HoleCardsAreDealtIntegrationEvent
        {
            Uid = integrationEvent.Uid,
            CorrelationUid = integrationEvent.CorrelationUid,
            OccurredAt = integrationEvent.OccurredAt,
            TableUid = integrationEvent.TableUid,
            HandUid = integrationEvent.HandUid,
            Nickname = integrationEvent.Nickname,
            Cards = string.Concat(Enumerable.Repeat("Xx", integrationEvent.Cards.Length / 2))
        };

        foreach (var player in table.Players)
        {
            var isTargetPlayer = player.Nickname == integrationEvent.Nickname;
            await connectionRegistry.SendIntegrationEventToPlayerAsync(
                tableUid: integrationEvent.TableUid,
                nickname: player.Nickname,
                integrationEvent: isTargetPlayer ? integrationEvent : anonymousIntegrationEvent
            );
        }
    }
}

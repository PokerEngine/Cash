using Application.Connection;
using Application.Repository;
using Application.Service.Hand;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.IntegrationEvent;

public record struct HandFinishedIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
}

public class HandFinishedHandler(
    IConnectionRegistry connectionRegistry,
    IRepository repository,
    IHandService handService
) : IIntegrationEventHandler<HandFinishedIntegrationEvent>
{
    public async Task HandleAsync(HandFinishedIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );

        var events = await repository.GetEventsAsync(integrationEvent.TableUid);
        var table = Table.FromEvents(integrationEvent.TableUid, events);

        table.FinishCurrentHand(integrationEvent.HandUid);

        if (table.HasEnoughPlayersForHand())
        {
            await StartHandAsync(table);
        }

        events = table.PullEvents();
        await repository.AddEventsAsync(table.Uid, events);
    }

    private async Task StartHandAsync(Table table)
    {
        table.RotateButton();

        var handUid = await handService.StartAsync(
            tableUid: table.Uid,
            game: table.Game,
            maxSeat: table.MaxSeat,
            smallBlind: table.SmallBlind,
            bigBlind: table.BigBlind,
            smallBlindSeat: table.SmallBlindSeat,
            bigBlindSeat: (Seat)table.BigBlindSeat!,
            buttonSeat: (Seat)table.ButtonSeat!,
            participants: table.ActivePlayers.Select(GetParticipant).ToList()
        );
        table.StartCurrentHand(handUid);
    }

    private HandParticipant GetParticipant(Player player)
    {
        return new HandParticipant
        {
            Nickname = player.Nickname,
            Seat = player.Seat,
            Stack = player.Stack
        };
    }
}

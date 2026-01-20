using Application.Connection;
using Application.Repository;
using Application.Service.Hand;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.IntegrationEvent;

public record struct PlayerSatDownIntegrationEvent : IIntegrationEvent
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
    IHandService handService
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
            await StartHandAsync(table);
        }
    }

    private async Task StartHandAsync(Table table)
    {
        table.RotateButton();

        var handUid = await handService.CreateAsync(
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

        table.SetCurrentHand(handUid);

        await handService.StartAsync(handUid);
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

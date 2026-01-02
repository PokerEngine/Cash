using Application.Event;
using Application.Repository;
using Application.Service.Hand;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.Command;

public record struct SitDownPlayerCommand : ICommand
{
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
}

public record struct SitDownPlayerResponse : ICommandResponse
{
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
}

public class SitDownPlayerHandler(
    IRepository repository,
    IEventDispatcher eventDispatcher,
    IHandService handService
) : ICommandHandler<SitDownPlayerCommand, SitDownPlayerResponse>
{
    public async Task<SitDownPlayerResponse> HandleAsync(SitDownPlayerCommand command)
    {
        var table = Table.FromEvents(
            uid: command.Uid,
            events: await repository.GetEventsAsync(command.Uid)
        );

        table.SitDown(command.Nickname, command.Seat, command.Stack);

        if (table.HasEnoughPlayersForHand() && !table.IsHandInProgress())
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
        }

        var events = table.PullEvents();
        await repository.AddEventsAsync(table.Uid, events);

        var context = new EventContext
        {
            TableUid = table.Uid
        };

        foreach (var @event in events)
        {
            await eventDispatcher.DispatchAsync(@event, context);
        }

        return new SitDownPlayerResponse
        {
            Uid = table.Uid,
            Nickname = command.Nickname,
        };
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

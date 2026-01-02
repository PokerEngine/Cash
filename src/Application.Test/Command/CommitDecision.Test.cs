using Application.Command;
using Application.Service.Hand;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Service.Hand;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.Test.Command;

public class CommitDecisionTest
{
    [Fact]
    public async Task HandleAsync_Valid_ShouldCommitDecision()
    {
        // Arrange
        var repository = new StubRepository();
        var eventDispatcher = new StubEventDispatcher();
        var handService = new StubHandService();
        var tableUid = await CreateTableAsync(repository, eventDispatcher);
        await SitDownPlayerAsync(
            repository: repository,
            eventDispatcher: eventDispatcher,
            tableUid: tableUid,
            nickname: "Alice",
            seat: 2,
            stack: 1000
        );
        await SitDownPlayerAsync(
            repository: repository,
            eventDispatcher: eventDispatcher,
            tableUid: tableUid,
            nickname: "Bobby",
            seat: 4,
            stack: 1000
        );
        await StartHandAsync(repository, handService, tableUid);

        var command = new CommitDecisionCommand
        {
            Uid = tableUid,
            Nickname = "Bobby",
            Type = "RaiseTo",
            Amount = 25
        };
        var handler = new CommitDecisionHandler(repository, handService);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.Uid, response.Uid);
        Assert.Equal(command.Nickname, response.Nickname);

        // TODO: perform taking chips from player's stack and adding to pot, and test it
    }

    private async Task<Guid> CreateTableAsync(StubRepository repository, StubEventDispatcher eventDispatcher)
    {
        var handler = new CreateTableHandler(repository, eventDispatcher);
        var command = new CreateTableCommand
        {
            Game = "NoLimitHoldem",
            MaxSeat = 6,
            SmallBlind = 5,
            BigBlind = 10,
            ChipCostAmount = 1,
            ChipCostCurrency = "Usd"
        };
        var response = await handler.HandleAsync(command);
        return response.Uid;
    }

    private async Task SitDownPlayerAsync(
        StubRepository repository,
        StubEventDispatcher eventDispatcher,
        Guid tableUid,
        string nickname,
        int seat,
        int stack
    )
    {
        var handler = new SitDownPlayerHandler(repository, eventDispatcher);
        var command = new SitDownPlayerCommand
        {
            Uid = tableUid,
            Nickname = nickname,
            Seat = seat,
            Stack = stack
        };
        await handler.HandleAsync(command);
    }

    private async Task StartHandAsync(
        StubRepository repository,
        StubHandService handService,
        Guid tableUid
    )
    {
        var table = Table.FromEvents(tableUid, await repository.GetEventsAsync(tableUid));
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

        await repository.AddEventsAsync(tableUid, table.PullEvents());
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

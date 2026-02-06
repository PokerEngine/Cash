using Application.Command;
using Application.Service.Hand;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Service.Hand;
using Application.Test.Storage;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.Test.Command;

public class SubmitPlayerActionTest
{
    [Fact]
    public async Task HandleAsync_Valid_ShouldSubmitPlayerAction()
    {
        // Arrange
        var repository = new StubRepository();
        var storage = new StubStorage();
        var eventDispatcher = new StubEventDispatcher();
        var handService = new StubHandService();
        var tableUid = await CreateTableAsync(repository, storage, eventDispatcher);
        await SitPlayerDownAsync(
            repository: repository,
            storage: storage,
            eventDispatcher: eventDispatcher,
            tableUid: tableUid,
            nickname: "Alice",
            seat: 2,
            stack: 1000
        );
        await SitPlayerDownAsync(
            repository: repository,
            storage: storage,
            eventDispatcher: eventDispatcher,
            tableUid: tableUid,
            nickname: "Bobby",
            seat: 4,
            stack: 1000
        );
        await StartHandAsync(repository, storage, handService, tableUid);

        var command = new SubmitPlayerActionCommand
        {
            Uid = tableUid,
            Nickname = "Bobby",
            Type = "RaiseBy",
            Amount = 20
        };
        var handler = new SubmitPlayerActionHandler(repository, handService);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.Uid, response.Uid);
        Assert.Equal(command.Nickname, response.Nickname);
    }

    private async Task<Guid> CreateTableAsync(
        StubRepository repository,
        StubStorage storage,
        StubEventDispatcher eventDispatcher)
    {
        var handler = new CreateTableHandler(repository, storage, eventDispatcher);
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

    private async Task SitPlayerDownAsync(
        StubRepository repository,
        StubStorage storage,
        StubEventDispatcher eventDispatcher,
        Guid tableUid,
        string nickname,
        int seat,
        int stack
    )
    {
        var handler = new SitPlayerDownHandler(repository, storage, eventDispatcher);
        var command = new SitPlayerDownCommand
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
        StubStorage storage,
        StubHandService handService,
        Guid tableUid
    )
    {
        var table = Table.FromEvents(tableUid, await repository.GetEventsAsync(tableUid));
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

        await repository.AddEventsAsync(tableUid, table.PullEvents());
        await storage.SaveViewAsync(table);
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

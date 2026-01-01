using Application.Command;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Service.Hand;
using Domain.Entity;
using Domain.Event;
using Domain.ValueObject;

namespace Application.Test.Command;

public class SitDownPlayerTest
{
    [Fact]
    public async Task HandleAsync_1stPlayer_ShouldSitDownAndWait()
    {
        // Arrange
        var repository = new StubRepository();
        var eventDispatcher = new StubEventDispatcher();
        var handService = new StubHandService();
        var tableUid = await CreateTableAsync(repository, eventDispatcher);
        await eventDispatcher.ClearDispatchedEvents(tableUid);

        var command = new SitDownPlayerCommand
        {
            Uid = tableUid,
            Nickname = "Alice",
            Seat = 2,
            Stack = 1000
        };
        var handler = new SitDownPlayerHandler(repository, eventDispatcher, handService);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.Uid, response.Uid);
        Assert.Equal(command.Nickname, response.Nickname);

        var table = Table.FromEvents(response.Uid, await repository.GetEventsAsync(response.Uid));
        Assert.False(table.IsHandInProgress());

        var player = table.Players.First();
        Assert.Equal(new Nickname("Alice"), player.Nickname);
        Assert.Equal(new Seat(2), player.Seat);
        Assert.Equal(new Chips(1000), player.Stack);

        var events = await eventDispatcher.GetDispatchedEvents(response.Uid);
        Assert.Single(events);
        Assert.IsType<PlayerSatDownEvent>(events[0]);
    }

    [Fact]
    public async Task HandleAsync_2ndPlayer_ShouldSitDownAndStartHand()
    {
        // Arrange
        var repository = new StubRepository();
        var eventDispatcher = new StubEventDispatcher();
        var handService = new StubHandService();
        var tableUid = await CreateTableAsync(repository, eventDispatcher);
        await SitDownPlayerAsync(
            repository: repository,
            eventDispatcher: eventDispatcher,
            handService: handService,
            tableUid: tableUid,
            nickname: "Alice",
            seat: 2,
            stack: 1000
        );

        var command = new SitDownPlayerCommand
        {
            Uid = tableUid,
            Nickname = "Bob",
            Seat = 4,
            Stack = 1000
        };
        var handler = new SitDownPlayerHandler(repository, eventDispatcher, handService);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.Uid, response.Uid);
        Assert.Equal(command.Nickname, response.Nickname);

        var table = Table.FromEvents(response.Uid, await repository.GetEventsAsync(response.Uid));
        Assert.True(table.IsHandInProgress());
        Assert.Equal(new Seat(2), table.ButtonSeat);
        Assert.Equal(new Seat(2), table.SmallBlindSeat);
        Assert.Equal(new Seat(4), table.BigBlindSeat);
    }

    [Fact]
    public async Task HandleAsync_NotExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var repository = new StubRepository();
        var eventDispatcher = new StubEventDispatcher();
        var handService = new StubHandService();

        var command = new SitDownPlayerCommand
        {
            Uid = new TableUid(Guid.NewGuid()),
            Nickname = "Alice",
            Seat = 2,
            Stack = 1000
        };
        var handler = new SitDownPlayerHandler(repository, eventDispatcher, handService);

        // Act
        var exc = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.HandleAsync(command);
        });

        // Assert
        Assert.Equal("The table is not found", exc.Message);

        var events = await eventDispatcher.GetDispatchedEvents(command.Uid);
        Assert.Empty(events);
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
        StubHandService handService,
        Guid tableUid,
        string nickname,
        int seat,
        int stack
    )
    {
        var handler = new SitDownPlayerHandler(repository, eventDispatcher, handService);
        var command = new SitDownPlayerCommand
        {
            Uid = tableUid,
            Nickname = nickname,
            Seat = seat,
            Stack = stack
        };
        await handler.HandleAsync(command);
    }
}

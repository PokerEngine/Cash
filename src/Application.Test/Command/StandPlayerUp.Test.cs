using Application.Command;
using Application.Test.Event;
using Application.Test.Repository;
using Domain.Entity;
using Domain.Event;
using Domain.ValueObject;

namespace Application.Test.Command;

public class StandPlayerUpTest
{
    [Fact]
    public async Task HandleAsync_Valid_ShouldSitDown()
    {
        // Arrange
        var repository = new StubRepository();
        var eventDispatcher = new StubEventDispatcher();
        var tableUid = await CreateTableAsync(repository, eventDispatcher);
        await SitPlayerDownAsync(
            repository: repository,
            eventDispatcher: eventDispatcher,
            tableUid: tableUid,
            nickname: "Alice",
            seat: 2,
            stack: 1000
        );
        await eventDispatcher.ClearDispatchedEvents(tableUid);

        var command = new StandPlayerUpCommand
        {
            Uid = tableUid,
            Nickname = "Alice"
        };
        var handler = new StandPlayerUpHandler(repository, eventDispatcher);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.Uid, response.Uid);
        Assert.Equal(command.Nickname, response.Nickname);

        var table = Table.FromEvents(response.Uid, await repository.GetEventsAsync(response.Uid));
        Assert.Empty(table.Players);

        var events = await eventDispatcher.GetDispatchedEvents(response.Uid);
        Assert.Single(events);
        Assert.IsType<PlayerStoodUpEvent>(events[0]);
    }

    [Fact]
    public async Task HandleAsync_NotExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var repository = new StubRepository();
        var eventDispatcher = new StubEventDispatcher();

        var command = new StandPlayerUpCommand
        {
            Uid = new TableUid(Guid.NewGuid()),
            Nickname = "Alice"
        };
        var handler = new StandPlayerUpHandler(repository, eventDispatcher);

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

    private async Task SitPlayerDownAsync(
        StubRepository repository,
        StubEventDispatcher eventDispatcher,
        Guid tableUid,
        string nickname,
        int seat,
        int stack
    )
    {
        var handler = new SitPlayerDownHandler(
            repository: repository,
            eventDispatcher: eventDispatcher
        );
        var command = new SitPlayerDownCommand
        {
            Uid = tableUid,
            Nickname = nickname,
            Seat = seat,
            Stack = stack
        };
        await handler.HandleAsync(command);
    }
}

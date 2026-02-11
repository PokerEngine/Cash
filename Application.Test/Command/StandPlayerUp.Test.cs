using Application.Command;
using Application.Exception;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Storage;
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
        var storage = new StubStorage();
        var eventDispatcher = new StubEventDispatcher();
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
        await eventDispatcher.ClearDispatchedEvents(tableUid);

        var command = new StandPlayerUpCommand
        {
            Uid = tableUid,
            Nickname = "Alice"
        };
        var handler = new StandPlayerUpHandler(repository, storage, eventDispatcher);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.Uid, response.Uid);
        Assert.Equal(command.Nickname, response.Nickname);

        var table = Table.FromEvents(response.Uid, await repository.GetEventsAsync(response.Uid));
        Assert.Empty(table.Players);

        var detailView = await storage.GetDetailViewAsync(table.Uid);
        Assert.Empty(detailView.Players);

        var listViews = await storage.GetListViewsAsync();
        Assert.Single(listViews);
        Assert.Equal(0, listViews[0].PlayerCount);

        var events = await eventDispatcher.GetDispatchedEvents(response.Uid);
        Assert.Single(events);
        Assert.IsType<PlayerStoodUpEvent>(events[0]);
    }

    [Fact]
    public async Task HandleAsync_NotExists_ShouldThrowException()
    {
        // Arrange
        var repository = new StubRepository();
        var storage = new StubStorage();
        var eventDispatcher = new StubEventDispatcher();

        var command = new StandPlayerUpCommand
        {
            Uid = new TableUid(Guid.NewGuid()),
            Nickname = "Alice"
        };
        var handler = new StandPlayerUpHandler(repository, storage, eventDispatcher);

        // Act
        var exc = await Assert.ThrowsAsync<TableNotFoundException>(async () =>
        {
            await handler.HandleAsync(command);
        });

        // Assert
        Assert.Equal("The table is not found", exc.Message);

        var events = await eventDispatcher.GetDispatchedEvents(command.Uid);
        Assert.Empty(events);
    }

    private async Task<Guid> CreateTableAsync(
        StubRepository repository,
        StubStorage storage,
        StubEventDispatcher eventDispatcher
    )
    {
        var handler = new CreateTableHandler(repository, storage, eventDispatcher);
        var command = new CreateTableCommand
        {
            Rules = new CreateTableCommandRules
            {
                Game = "NoLimitHoldem",
                MaxSeat = 6,
                SmallBlind = 5,
                BigBlind = 10,
                ChipCostAmount = 1,
                ChipCostCurrency = "Usd"
            }
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
}

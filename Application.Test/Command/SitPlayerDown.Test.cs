using Application.Command;
using Application.Exception;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Storage;
using Application.Test.UnitOfWork;
using Domain.Entity;
using Domain.Event;
using Domain.ValueObject;

namespace Application.Test.Command;

public class SitPlayerDownTest
{
    [Fact]
    public async Task HandleAsync_Valid_ShouldSitDown()
    {
        // Arrange
        var unitOfWork = CreateUnitOfWork();
        var tableUid = await CreateTableAsync(unitOfWork);

        var command = new SitPlayerDownCommand
        {
            Uid = tableUid,
            Nickname = "Alice",
            Seat = 2,
            Stack = 1000
        };
        var handler = new SitPlayerDownHandler(unitOfWork.Repository, unitOfWork);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.Uid, response.Uid);
        Assert.Equal(command.Nickname, response.Nickname);

        var table = Table.FromEvents(response.Uid, await unitOfWork.Repository.GetEventsAsync(response.Uid));
        Assert.False(table.IsHandInProgress());

        var player = table.Players.First();
        Assert.Equal(new Nickname("Alice"), player.Nickname);
        Assert.Equal(new Seat(2), player.Seat);
        Assert.Equal(new Chips(1000), player.Stack);

        var detailView = await unitOfWork.Storage.GetDetailViewAsync(table.Uid);
        Assert.Single(detailView.Players);
        Assert.Equal(new Nickname("Alice"), detailView.Players[0].Nickname);

        var listViews = await unitOfWork.Storage.GetListViewsAsync();
        Assert.Single(listViews);
        Assert.Equal(1, listViews[0].PlayerCount);

        var events = await unitOfWork.EventDispatcher.GetDispatchedEvents(response.Uid);
        Assert.Single(events);
        Assert.IsType<PlayerSatDownEvent>(events[0]);
    }

    [Fact]
    public async Task HandleAsync_NotExists_ShouldThrowException()
    {
        // Arrange
        var unitOfWork = CreateUnitOfWork();

        var command = new SitPlayerDownCommand
        {
            Uid = new TableUid(Guid.NewGuid()),
            Nickname = "Alice",
            Seat = 2,
            Stack = 1000
        };
        var handler = new SitPlayerDownHandler(unitOfWork.Repository, unitOfWork);

        // Act
        var exc = await Assert.ThrowsAsync<TableNotFoundException>(async () =>
        {
            await handler.HandleAsync(command);
        });

        // Assert
        Assert.Equal("The table is not found", exc.Message);

        var events = await unitOfWork.EventDispatcher.GetDispatchedEvents(command.Uid);
        Assert.Empty(events);
    }

    private async Task<Guid> CreateTableAsync(StubUnitOfWork unitOfWork)
    {
        var handler = new CreateTableHandler(unitOfWork.Repository, unitOfWork);
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
        await unitOfWork.EventDispatcher.ClearDispatchedEvents(response.Uid);
        return response.Uid;
    }

    private StubUnitOfWork CreateUnitOfWork()
    {
        var repository = new StubRepository();
        var storage = new StubStorage();
        var eventDispatcher = new StubEventDispatcher();
        return new StubUnitOfWork(repository, storage, eventDispatcher);
    }
}

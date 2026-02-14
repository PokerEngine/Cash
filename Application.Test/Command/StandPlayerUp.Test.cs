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

public class StandPlayerUpTest
{
    [Fact]
    public async Task HandleAsync_Valid_ShouldSitDown()
    {
        // Arrange
        var unitOfWork = CreateUnitOfWork();
        var tableUid = await CreateTableAsync(unitOfWork);
        await SitPlayerDownAsync(unitOfWork, tableUid, "Alice", 2, 1000);

        var command = new StandPlayerUpCommand
        {
            Uid = tableUid,
            Nickname = "Alice"
        };
        var handler = new StandPlayerUpHandler(unitOfWork.Repository, unitOfWork);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.Uid, response.Uid);
        Assert.Equal(command.Nickname, response.Nickname);

        var table = Table.FromEvents(response.Uid, await unitOfWork.Repository.GetEventsAsync(response.Uid));
        Assert.Empty(table.Players);

        var detailView = await unitOfWork.Storage.GetDetailViewAsync(table.Uid);
        Assert.Empty(detailView.Players);

        var listViews = await unitOfWork.Storage.GetListViewsAsync();
        Assert.Single(listViews);
        Assert.Equal(0, listViews[0].PlayerCount);

        var events = await unitOfWork.EventDispatcher.GetDispatchedEvents(response.Uid);
        Assert.Single(events);
        Assert.IsType<PlayerStoodUpEvent>(events[0]);
    }

    [Fact]
    public async Task HandleAsync_NotExists_ShouldThrowException()
    {
        // Arrange
        var unitOfWork = CreateUnitOfWork();

        var command = new StandPlayerUpCommand
        {
            Uid = new TableUid(Guid.NewGuid()),
            Nickname = "Alice"
        };
        var handler = new StandPlayerUpHandler(unitOfWork.Repository, unitOfWork);

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

    private async Task SitPlayerDownAsync(
        StubUnitOfWork unitOfWork,
        Guid tableUid,
        string nickname,
        int seat,
        int stack
    )
    {
        var handler = new SitPlayerDownHandler(unitOfWork.Repository, unitOfWork);
        var command = new SitPlayerDownCommand
        {
            Uid = tableUid,
            Nickname = nickname,
            Seat = seat,
            Stack = stack
        };
        await handler.HandleAsync(command);
        await unitOfWork.EventDispatcher.ClearDispatchedEvents(tableUid);
    }

    private StubUnitOfWork CreateUnitOfWork()
    {
        var repository = new StubRepository();
        var storage = new StubStorage();
        var eventDispatcher = new StubEventDispatcher();
        return new StubUnitOfWork(repository, storage, eventDispatcher);
    }
}

using Application.Command;
using Application.Exception;
using Application.Query;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Service.Hand;
using Application.Test.Storage;
using Application.Test.UnitOfWork;

namespace Application.Test.Query;

public class GetTableDetailTest
{
    [Fact]
    public async Task HandleAsync_Exists_ShouldReturn()
    {
        // Arrange
        var unitOfWork = CreateUnitOfWork();
        var handService = new StubHandService();
        var tableUid = await CreateTableAsync(unitOfWork);
        await SitPlayerDownAsync(unitOfWork, tableUid, "Alice", 2, 1000);

        var query = new GetTableDetailQuery { Uid = tableUid };
        var handler = new GetTableDetailHandler(unitOfWork.Storage, handService);

        // Act
        var response = await handler.HandleAsync(query);

        // Assert
        Assert.Single(response.Players);
        Assert.Equal("Alice", response.Players[0].Nickname);
        Assert.Equal(2, response.Players[0].Seat);
        Assert.Equal(1000, response.Players[0].Stack);
        Assert.False(response.Players[0].IsSittingOut);

        Assert.Null(response.CurrentHand);
    }

    [Fact]
    public async Task HandleAsync_NotExists_ShouldThrowException()
    {
        // Arrange
        var storage = new StubStorage();
        var handService = new StubHandService();

        var query = new GetTableDetailQuery { Uid = Guid.NewGuid() };
        var handler = new GetTableDetailHandler(storage, handService);

        // Act
        var exc = await Assert.ThrowsAsync<TableNotFoundException>(async () =>
        {
            await handler.HandleAsync(query);
        });

        // Assert
        Assert.Equal("The table is not found", exc.Message);
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
        await unitOfWork.EventDispatcher.ClearDispatchedEvents(tableUid);
        await handler.HandleAsync(command);
    }

    private StubUnitOfWork CreateUnitOfWork()
    {
        var repository = new StubRepository();
        var storage = new StubStorage();
        var eventDispatcher = new StubEventDispatcher();
        return new StubUnitOfWork(repository, storage, eventDispatcher);
    }
}

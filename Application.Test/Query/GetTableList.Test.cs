using Application.Command;
using Application.Query;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Storage;
using Application.Test.UnitOfWork;

namespace Application.Test.Query;

public class GetTableListTest
{
    [Fact]
    public async Task HandleAsync_WhenExists_ShouldReturn()
    {
        // Arrange
        var unitOfWork = CreateUnitOfWork();
        var tableUid = await CreateTableAsync(unitOfWork);
        await SitPlayerDownAsync(unitOfWork, tableUid, "Alice", 2, 1000);

        var query = new GetTableListQuery();
        var handler = new GetTableListHandler(unitOfWork.Storage);

        // Act
        var response = await handler.HandleAsync(query);

        // Assert
        Assert.Single(response.Items);
        Assert.Equal(tableUid, response.Items[0].Uid);
        Assert.Equal("NoLimitHoldem", response.Items[0].Rules.Game);
        Assert.Equal(6, response.Items[0].Rules.MaxSeat);
        Assert.Equal(1000, response.Items[0].Rules.Stake);
        Assert.Equal(1, response.Items[0].PlayerCount);
    }

    [Fact]
    public async Task HandleAsync_NotExists_ShouldReturnEmpty()
    {
        // Arrange
        var storage = new StubStorage();

        var query = new GetTableListQuery();
        var handler = new GetTableListHandler(storage);

        // Act
        var response = await handler.HandleAsync(query);

        // Assert
        Assert.Empty(response.Items);
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

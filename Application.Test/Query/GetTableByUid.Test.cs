using Application.Command;
using Application.Exception;
using Application.Query;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Service.Hand;
using Application.Test.Storage;
using Domain.Entity;

namespace Application.Test.Query;

public class GetTableByUidTest
{
    [Fact]
    public async Task HandleAsync_Exists_ShouldReturn()
    {
        // Arrange
        var repository = new StubRepository();
        var storage = new StubStorage();
        var eventDispatcher = new StubEventDispatcher();
        var handService = new StubHandService();
        var tableUid = await CreateTableAsync(repository, storage, eventDispatcher);
        await SitPlayerDownAsync(repository, storage, eventDispatcher, tableUid, "Alice", 2, 1000);

        var query = new GetTableByUidQuery { Uid = tableUid };
        var handler = new GetTableByUidHandler(
            storage: storage,
            handService: handService
        );

        // Act
        var response = await handler.HandleAsync(query);

        // Assert
        var table = Table.FromEvents(query.Uid, await repository.GetEventsAsync(query.Uid));
        Assert.Equal((Guid)table.Uid, response.Uid);
        Assert.Equal("NoLimitHoldem", response.Game);
        Assert.Equal(6, response.MaxSeat);
        Assert.Equal(5, response.SmallBlind);
        Assert.Equal(10, response.BigBlind);

        Assert.Single(response.Players);
        Assert.Equal("Alice", response.Players[0].Nickname);
        Assert.Equal(2, response.Players[0].Seat);
        Assert.Equal(1000, response.Players[0].Stack);
        Assert.False(response.Players[0].IsSittingOut);

        Assert.Null(response.HandState);
    }

    [Fact]
    public async Task HandleAsync_NotExists_ShouldThrowException()
    {
        // Arrange
        var storage = new StubStorage();
        var handService = new StubHandService();

        var query = new GetTableByUidQuery { Uid = Guid.NewGuid() };
        var handler = new GetTableByUidHandler(storage, handService);

        // Act
        var exc = await Assert.ThrowsAsync<TableNotFoundException>(async () =>
        {
            await handler.HandleAsync(query);
        });

        // Assert
        Assert.Equal("The table is not found", exc.Message);
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
}

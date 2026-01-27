using Application.Command;
using Application.Query;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Service.Hand;
using Domain.Entity;

namespace Application.Test.Query;

public class GetTableByUidTest
{
    [Fact]
    public async Task HandleAsync_Exists_ShouldReturn()
    {
        // Arrange
        var repository = new StubRepository();
        var eventDispatcher = new StubEventDispatcher();
        var handService = new StubHandService();
        var tableUid = await CreateTableAsync(repository, eventDispatcher);
        await SitPlayerDownAsync(repository, eventDispatcher, tableUid, "Alice", 2, 1000);

        var query = new GetTableByUidQuery { Uid = tableUid };
        var handler = new GetTableByUidHandler(
            repository: repository,
            handService: handService
        );

        // Act
        var response = await handler.HandleAsync(query);

        // Assert
        var table = Table.FromEvents(query.Uid, await repository.GetEventsAsync(query.Uid));
        Assert.Equal((Guid)table.Uid, response.Uid);
        Assert.Equal(table.Game.ToString(), response.Game);
        Assert.Equal((int)table.MaxSeat, response.MaxSeat);
        Assert.Equal((int)table.SmallBlind, response.SmallBlind);
        Assert.Equal((int)table.BigBlind, response.BigBlind);
        Assert.Equal(table.ChipCost.Amount, response.ChipCostAmount);
        Assert.Equal(table.ChipCost.Currency.ToString(), response.ChipCostCurrency);

        Assert.Single(response.Players);
        Assert.Equal("Alice", response.Players[0].Nickname);
        Assert.Equal(2, response.Players[0].Seat);
        Assert.Equal(1000, response.Players[0].Stack);
        Assert.False(response.Players[0].IsSittingOut);

        Assert.Null(response.HandState);
    }

    [Fact]
    public async Task HandleAsync_NotExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var repository = new StubRepository();
        var handService = new StubHandService();

        var query = new GetTableByUidQuery { Uid = Guid.NewGuid() };
        var handler = new GetTableByUidHandler(
            repository: repository,
            handService: handService
        );

        // Act
        var exc = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.HandleAsync(query);
        });

        // Assert
        Assert.Equal("The table is not found", exc.Message);
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

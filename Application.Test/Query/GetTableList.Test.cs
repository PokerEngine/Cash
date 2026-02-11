using Application.Command;
using Application.Query;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Storage;

namespace Application.Test.Query;

public class GetTableListTest
{
    [Fact]
    public async Task HandleAsync_WhenExists_ShouldReturn()
    {
        // Arrange
        var repository = new StubRepository();
        var storage = new StubStorage();
        var eventDispatcher = new StubEventDispatcher();
        var tableUid = await CreateTableAsync(repository, storage, eventDispatcher);
        await SitPlayerDownAsync(repository, storage, eventDispatcher, tableUid, "Alice", 2, 1000);

        var query = new GetTableListQuery();
        var handler = new GetTableListHandler(storage);

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

using Application.Exception;
using Application.Storage;
using Domain.Entity;
using Domain.ValueObject;
using Infrastructure.Storage;
using Infrastructure.Test.Client.MongoDb;
using Microsoft.Extensions.Options;

namespace Infrastructure.Test.Storage;

[Trait("Category", "Integration")]
public class MongoDbStorageTest(MongoDbClientFixture fixture) : IClassFixture<MongoDbClientFixture>
{
    [Fact]
    public async Task GetDetailViewAsync_WhenExists_ShouldReturn()
    {
        // Arrange
        var storage = CreateStorage();
        var table = CreateTable();
        table.SitPlayerDown(new Nickname("Alice"), new Seat(1), new Chips(1000));
        table.SitPlayerDown(new Nickname("Bobby"), new Seat(2), new Chips(900));
        table.SitPlayerOut(new Nickname("Bobby"));
        table.StartCurrentHand(Guid.NewGuid());
        await storage.SaveViewAsync(table);

        // Act
        var view = await storage.GetDetailViewAsync(table.Uid);

        // Assert
        Assert.Equal(table.Uid, view.Uid);
        Assert.Equal(Game.NoLimitHoldem, view.Game);
        Assert.Equal(new Seat(6), view.MaxSeat);
        Assert.Equal(new Money(1000, Currency.Usd), view.Stake);
        Assert.Equal(new Money(5, Currency.Usd), view.SmallBlind);
        Assert.Equal(new Money(10, Currency.Usd), view.BigBlind);
        Assert.Equal(table.GetCurrentHandUid(), view.CurrentHandUid);

        Assert.Equal(2, view.Players.Count);
        Assert.Equal(new Nickname("Alice"), view.Players[0].Nickname);
        Assert.Equal(new Seat(1), view.Players[0].Seat);
        Assert.Equal(new Money(1000, Currency.Usd), view.Players[0].Stack);
        Assert.False(view.Players[0].IsSittingOut);
        Assert.Equal(new Nickname("Bobby"), view.Players[1].Nickname);
        Assert.Equal(new Seat(2), view.Players[1].Seat);
        Assert.Equal(new Money(900, Currency.Usd), view.Players[1].Stack);
        Assert.True(view.Players[1].IsSittingOut);
    }

    [Fact]
    public async Task GetDetailViewAsync_WhenNotExists_ShouldThrowException()
    {
        // Arrange
        var storage = CreateStorage();

        // Act
        var exc = await Assert.ThrowsAsync<TableNotFoundException>(async () =>
            await storage.GetDetailViewAsync(new TableUid(Guid.NewGuid())));

        // Assert
        Assert.Equal("The table is not found", exc.Message);
    }

    [Fact]
    public async Task GetListViewsAsync_NoFilter_ShouldReturnAll()
    {
        // Arrange
        var storage = CreateStorage();
        var table6MaxNlh1000 = CreateTable();
        table6MaxNlh1000.SitPlayerDown(new Nickname("Alice"), new Seat(1), new Chips(1000));
        table6MaxNlh1000.SitPlayerDown(new Nickname("Bobby"), new Seat(2), new Chips(1000));
        var table9MaxPlo2000 = CreateTable(Game.PotLimitOmaha, 9, 10, 20);
        table9MaxPlo2000.SitPlayerDown(new Nickname("Alice"), new Seat(1), new Chips(2000));
        await storage.SaveViewAsync(table6MaxNlh1000);
        await storage.SaveViewAsync(table9MaxPlo2000);

        // Act
        var views = await storage.GetListViewsAsync();

        // Assert
        Assert.Equal(2, views.Count);
        Assert.Equal(table6MaxNlh1000.Uid, views[0].Uid);
        Assert.Equal(Game.NoLimitHoldem, views[0].Game);
        Assert.Equal(new Seat(6), views[0].MaxSeat);
        Assert.Equal(new Money(1000, Currency.Usd), views[0].Stake);
        Assert.Equal(2, views[0].PlayerCount);
        Assert.Equal(table9MaxPlo2000.Uid, views[1].Uid);
        Assert.Equal(Game.PotLimitOmaha, views[1].Game);
        Assert.Equal(new Seat(9), views[1].MaxSeat);
        Assert.Equal(new Money(2000, Currency.Usd), views[1].Stake);
        Assert.Equal(1, views[1].PlayerCount);
    }

    [Fact]
    public async Task GetListViewsAsync_FilterByHasPlayers_ShouldReturnFiltered()
    {
        // Arrange
        var storage = CreateStorage();
        var table6MaxNlh1000 = CreateTable();
        table6MaxNlh1000.SitPlayerDown(new Nickname("Alice"), new Seat(1), new Chips(1000));
        var table9MaxPlo2000 = CreateTable(Game.PotLimitOmaha, 9, 10, 20);
        await storage.SaveViewAsync(table6MaxNlh1000);
        await storage.SaveViewAsync(table9MaxPlo2000);

        // Act
        var views = await storage.GetListViewsAsync(hasPlayersOnly: true);

        // Assert
        Assert.Single(views);
        Assert.Equal(table6MaxNlh1000.Uid, views[0].Uid);
        Assert.Equal(1, views[0].PlayerCount);
    }

    [Fact]
    public async Task GetListViewsAsync_FilterByGame_ShouldReturnFiltered()
    {
        // Arrange
        var storage = CreateStorage();
        var table6MaxNlh1000 = CreateTable();
        var table9MaxPlo2000 = CreateTable(Game.PotLimitOmaha, 9, 10, 20);
        await storage.SaveViewAsync(table6MaxNlh1000);
        await storage.SaveViewAsync(table9MaxPlo2000);

        // Act
        var views = await storage.GetListViewsAsync(games: [Game.NoLimitHoldem]);

        // Assert
        Assert.Single(views);
        Assert.Equal(table6MaxNlh1000.Uid, views[0].Uid);
        Assert.Equal(Game.NoLimitHoldem, views[0].Game);
    }

    [Fact]
    public async Task GetListViewsAsync_FilterByMinStake_ShouldReturnFiltered()
    {
        // Arrange
        var storage = CreateStorage();
        var table6MaxNlh1000 = CreateTable();
        var table9MaxPlo2000 = CreateTable(Game.PotLimitOmaha, 9, 10, 20);
        await storage.SaveViewAsync(table6MaxNlh1000);
        await storage.SaveViewAsync(table9MaxPlo2000);

        // Act
        var views = await storage.GetListViewsAsync(minStake: new Money(1001, Currency.Usd));

        // Assert
        Assert.Single(views);
        Assert.Equal(table9MaxPlo2000.Uid, views[0].Uid);
        Assert.Equal(new Money(2000, Currency.Usd), views[0].Stake);
    }

    [Fact]
    public async Task GetListViewsAsync_FilterByMaxStake_ShouldReturnFiltered()
    {
        // Arrange
        var storage = CreateStorage();
        var table6MaxNlh1000 = CreateTable();
        var table9MaxPlo2000 = CreateTable(Game.PotLimitOmaha, 9, 10, 20);
        await storage.SaveViewAsync(table6MaxNlh1000);
        await storage.SaveViewAsync(table9MaxPlo2000);

        // Act
        var views = await storage.GetListViewsAsync(maxStake: new Money(1999, Currency.Usd));

        // Assert
        Assert.Single(views);
        Assert.Equal(table6MaxNlh1000.Uid, views[0].Uid);
        Assert.Equal(new Money(1000, Currency.Usd), views[0].Stake);
    }

    private IStorage CreateStorage()
    {
        var client = fixture.CreateClient();
        var options = CreateOptions();
        return new MongoDbStorage(client, options);
    }

    private IOptions<MongoDbStorageOptions> CreateOptions()
    {
        var options = new MongoDbStorageOptions
        {
            Database = $"test_storage_{Guid.NewGuid()}"
        };
        return Options.Create(options);
    }

    private Table CreateTable(
        Game game = Game.NoLimitHoldem,
        int maxSeat = 6,
        int smallBlind = 5,
        int bigBlind = 10
    )
    {
        return Table.FromScratch(
            uid: new TableUid(Guid.NewGuid()),
            game: game,
            maxSeat: maxSeat,
            smallBlind: smallBlind,
            bigBlind: bigBlind,
            chipCost: new Money(1, Currency.Usd)
        );
    }
}

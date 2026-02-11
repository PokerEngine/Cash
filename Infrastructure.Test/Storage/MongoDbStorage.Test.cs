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
        Assert.Equal((Guid)table.Uid, view.Uid);
        Assert.Equal("NoLimitHoldem", view.Rules.Game);
        Assert.Equal(6, view.Rules.MaxSeat);
        Assert.Equal(1000, view.Rules.Stake);
        Assert.Equal(5, view.Rules.SmallBlind);
        Assert.Equal(10, view.Rules.BigBlind);
        Assert.Equal(table.GetCurrentHandUid(), view.CurrentHandUid);

        Assert.Equal(2, view.Players.Count);
        Assert.Equal("Alice", view.Players[0].Nickname);
        Assert.Equal(1, view.Players[0].Seat);
        Assert.Equal(1000.0, (float)view.Players[0].Stack);
        Assert.False(view.Players[0].IsSittingOut);
        Assert.Equal("Bobby", view.Players[1].Nickname);
        Assert.Equal(2, view.Players[1].Seat);
        Assert.Equal(900.0, (float)view.Players[1].Stack);
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
        Assert.Equal((Guid)table6MaxNlh1000.Uid, views[0].Uid);
        Assert.Equal("NoLimitHoldem", views[0].Rules.Game);
        Assert.Equal(6, views[0].Rules.MaxSeat);
        Assert.Equal(1000, views[0].Rules.Stake);
        Assert.Equal(2, views[0].PlayerCount);
        Assert.Equal((Guid)table9MaxPlo2000.Uid, views[1].Uid);
        Assert.Equal("PotLimitOmaha", views[1].Rules.Game);
        Assert.Equal(9, views[1].Rules.MaxSeat);
        Assert.Equal(2000, views[1].Rules.Stake);
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
        Assert.Equal((Guid)table6MaxNlh1000.Uid, views[0].Uid);
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
        var views = await storage.GetListViewsAsync(games: ["NoLimitHoldem"]);

        // Assert
        Assert.Single(views);
        Assert.Equal((Guid)table6MaxNlh1000.Uid, views[0].Uid);
        Assert.Equal("NoLimitHoldem", views[0].Rules.Game);
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
        var views = await storage.GetListViewsAsync(minStake: 1001);

        // Assert
        Assert.Single(views);
        Assert.Equal((Guid)table9MaxPlo2000.Uid, views[0].Uid);
        Assert.Equal(2000, views[0].Rules.Stake);
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
        var views = await storage.GetListViewsAsync(maxStake: 1999);

        // Assert
        Assert.Single(views);
        Assert.Equal((Guid)table6MaxNlh1000.Uid, views[0].Uid);
        Assert.Equal(1000, views[0].Rules.Stake);
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
            rules: new Rules
            {
                Game = game,
                MaxSeat = maxSeat,
                SmallBlind = smallBlind,
                BigBlind = bigBlind,
                ChipCost = new Money(1, Currency.Usd)
            }
        );
    }
}

using Domain.Entity;
using Domain.Event;
using Domain.ValueObject;

namespace Domain.Test.Entity;

public class TableTest
{
    [Theory]
    [InlineData(2, 2, 1)] // Heads-up
    [InlineData(6, 1, 2)] // 6max
    public void FromScratch_Valid_ShouldCreate(int maxSeat, int smallBlindSeat, int bigBlindSeat)
    {
        // Arrange & Act
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var uid = new TableUid(Guid.NewGuid());
        var table = Table.FromScratch(
            uid: uid,
            game: Game.NoLimitHoldem,
            smallBlind: new Chips(5),
            bigBlind: new Chips(10),
            chipCost: new Money(1, Currency.Usd),
            maxSeat: new Seat(maxSeat),
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(uid, table.Uid);
        Assert.Equal(Game.NoLimitHoldem, table.Game);
        Assert.Equal(new Chips(5), table.SmallBlind);
        Assert.Equal(new Chips(10), table.BigBlind);
        Assert.Equal(new Money(1, Currency.Usd), table.ChipCost);
        Assert.Equal(new Seat(maxSeat), table.MaxSeat);
        Assert.Equal(new Seat(maxSeat), table.ButtonSeat);
        Assert.Equal(new Seat(smallBlindSeat), table.SmallBlindSeat);
        Assert.Equal(new Seat(bigBlindSeat), table.BigBlindSeat);
        Assert.Empty(table.Players);

        Assert.Single(events);
        var @event = Assert.IsType<TableIsCreatedEvent>(events[0]);
        Assert.Equal(uid, @event.Uid);
        Assert.Equal(Game.NoLimitHoldem, @event.Game);
        Assert.Equal(new Chips(5), @event.SmallBlind);
        Assert.Equal(new Chips(10), @event.BigBlind);
        Assert.Equal(new Money(1, Currency.Usd), @event.ChipCost);
        Assert.Equal(new Seat(maxSeat), @event.MaxSeat);
    }

    [Fact]
    public void SitDown_IsNotSittingDown_ShouldSitDown()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();

        // Act
        table.SitDown(
            nickname: new Nickname("SmallBlind"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: eventBus
        );

        // Assert
        Assert.Single(table.Players);
        var player = table.Players.First();
        Assert.Equal(new Nickname("SmallBlind"), player.Nickname);
        Assert.Equal(new Seat(1), player.Seat);
        Assert.Equal(new Chips(1000), player.Stack);
        Assert.True(player.IsWaitingForBigBlind);
        Assert.False(player.IsDisconnected);
        Assert.False(player.IsSittingOut);

        Assert.Single(events);
        var @event = Assert.IsType<PlayerSatDownEvent>(events[0]);
        Assert.Equal(new Nickname("SmallBlind"), @event.Nickname);
        Assert.Equal(new Seat(1), @event.Seat);
        Assert.Equal(new Chips(1000), @event.Stack);
    }

    [Fact]
    public void SitDown_IsSittingDown_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("SmallBlind"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitDown(
                nickname: new Nickname("SmallBlind"),
                seat: new Seat(2),
                stack: new Chips(1000),
                eventBus: eventBus
            );
        });

        // Assert
        Assert.Equal("A player with the given nickname is already sitting down at the table", exc.Message);
        Assert.Empty(events);
    }

    [Fact]
    public void SitDown_SeatIsOccupied_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("SmallBlind"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitDown(
                nickname: new Nickname("BigBlind"),
                seat: new Seat(1),
                stack: new Chips(1000),
                eventBus: eventBus
            );
        });

        // Assert
        Assert.Equal("The seat has already been occupied at the table", exc.Message);
        Assert.Empty(events);
    }

    [Fact]
    public void SitDown_SeatIsOutOfRange_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitDown(
                nickname: new Nickname("SmallBlind"),
                seat: new Seat(7),
                stack: new Chips(1000),
                eventBus: eventBus
            );
        });

        // Assert
        Assert.Equal("The table supports seats till #6", exc.Message);
        Assert.Empty(events);
    }

    [Fact]
    public void StandUp_IsSittingDown_ShouldStandUp()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("SmallBlind"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        table.StandUp(
            nickname: new Nickname("SmallBlind"),
            eventBus: eventBus
        );

        // Assert
        Assert.Empty(table.Players);

        Assert.Single(events);
        var @event = Assert.IsType<PlayerStoodUpEvent>(events[0]);
        Assert.Equal(new Nickname("SmallBlind"), @event.Nickname);
    }

    [Fact]
    public void StandUp_IsNotSittingDown_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.StandUp(
                nickname: new Nickname("SmallBlind"),
                eventBus: eventBus
            );
        });

        // Assert
        Assert.Equal("A player with the given nickname is not found at the table", exc.Message);
        Assert.Empty(events);
    }

    [Fact]
    public void SitOut_IsNotSittingOut_ShouldSitOut()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("SmallBlind"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        table.SitOut(
            nickname: new Nickname("SmallBlind"),
            eventBus: eventBus
        );

        // Assert
        var player = table.Players.First();
        Assert.True(player.IsSittingOut);
        Assert.False(player.IsWaitingForBigBlind);

        Assert.Single(events);
        var @event = Assert.IsType<PlayerSatOutEvent>(events[0]);
        Assert.Equal(new Nickname("SmallBlind"), @event.Nickname);
    }

    [Fact]
    public void SitOut_IsSittingOut_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("SmallBlind"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitOut(
            nickname: new Nickname("SmallBlind"),
            eventBus: new EventBus()
        );

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitOut(
                nickname: new Nickname("SmallBlind"),
                eventBus: eventBus
            );
        });

        // Assert
        Assert.Equal("The player is already sitting out", exc.Message);
        Assert.Empty(events);
    }

    [Fact]
    public void SitOut_IsNotSittingDown_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitOut(
                nickname: new Nickname("SmallBlind"),
                eventBus: eventBus
            );
        });

        // Assert
        Assert.Equal("A player with the given nickname is not found at the table", exc.Message);
        Assert.Empty(events);
    }

    [Fact]
    public void SitIn_IsSittingOut_ShouldSitIn()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("SmallBlind"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitOut(
            nickname: new Nickname("SmallBlind"),
            eventBus: new EventBus()
        );

        // Act
        table.SitIn(
            nickname: new Nickname("SmallBlind"),
            eventBus: eventBus
        );

        // Assert
        var player = table.Players.First();
        Assert.False(player.IsSittingOut);
        Assert.True(player.IsWaitingForBigBlind);

        var @event = Assert.IsType<PlayerSatInEvent>(events[0]);
        Assert.Equal(new Nickname("SmallBlind"), @event.Nickname);
    }

    [Fact]
    public void SitIn_IsNotSittingOut_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("SmallBlind"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitIn(
                nickname: new Nickname("SmallBlind"),
                eventBus: eventBus
            );
        });

        // Assert
        Assert.Equal("The player is not sitting out yet", exc.Message);
        Assert.Empty(events);
    }

    [Fact]
    public void SitIn_IsNotSittingDown_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitIn(
                nickname: new Nickname("SmallBlind"),
                eventBus: eventBus
            );
        });

        // Assert
        Assert.Equal("A player with the given nickname is not found at the table", exc.Message);
        Assert.Empty(events);
    }

    private Table CreateTable(
        int maxSeat = 6,
        int smallBlind = 5,
        int bigBlind = 10,
        decimal chipCost = 1
    )
    {
        var eventBus = new EventBus();
        return Table.FromScratch(
            uid: new TableUid(Guid.NewGuid()),
            game: Game.NoLimitHoldem,
            smallBlind: new Chips(smallBlind),
            bigBlind: new Chips(bigBlind),
            maxSeat: new Seat(maxSeat),
            chipCost: new Money(chipCost, Currency.Usd),
            eventBus: eventBus
        );
    }
}

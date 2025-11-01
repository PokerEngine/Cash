using Domain.Entity;
using Domain.Event;
using Domain.ValueObject;

namespace Domain.Test.Entity;

public class TableTest
{
    [Theory]
    [InlineData(2)]
    [InlineData(6)]
    [InlineData(9)]
    public void FromScratch_Valid_ShouldCreate(int maxSeat)
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        // Act
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
        Assert.Null(table.ButtonSeat);
        Assert.Null(table.SmallBlindSeat);
        Assert.Null(table.BigBlindSeat);
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
    public void SitDown_1stPlayer_ShouldSitDownAndNotWaitForBigBlind()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();

        // Act
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: eventBus
        );

        // Assert
        Assert.Single(table.Players);
        var player = table.Players.First();
        Assert.Equal(new Nickname("Alice"), player.Nickname);
        Assert.Equal(new Seat(1), player.Seat);
        Assert.Equal(new Chips(1000), player.Stack);
        Assert.False(player.IsWaitingForBigBlind);
        Assert.False(player.IsDisconnected);
        Assert.False(player.IsSittingOut);

        Assert.Single(events);
        var @event = Assert.IsType<PlayerSatDownEvent>(events[0]);
        Assert.Equal(new Nickname("Alice"), @event.Nickname);
        Assert.Equal(new Seat(1), @event.Seat);
        Assert.Equal(new Chips(1000), @event.Stack);
        Assert.False(@event.IsWaitingForBigBlind);
    }

    [Fact]
    public void SitDown_2ndPlayer_ShouldSitDownAndNotWaitForBigBlind()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(2, table.Players.Count());
        var player = table.Players.First(x => x.Seat == new Seat(2));
        Assert.Equal(new Nickname("Bob"), player.Nickname);
        Assert.Equal(new Seat(2), player.Seat);
        Assert.Equal(new Chips(1000), player.Stack);
        Assert.False(player.IsWaitingForBigBlind);
        Assert.False(player.IsDisconnected);
        Assert.False(player.IsSittingOut);

        Assert.Single(events);
        var @event = Assert.IsType<PlayerSatDownEvent>(events[0]);
        Assert.Equal(new Nickname("Bob"), @event.Nickname);
        Assert.Equal(new Seat(2), @event.Seat);
        Assert.Equal(new Chips(1000), @event.Stack);
        Assert.False(@event.IsWaitingForBigBlind);
    }

    [Fact]
    public void SitDown_3rdPlayerInitial_ShouldSitDownAndNotWaitForBigBlind()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(3),
            stack: new Chips(1000),
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(3, table.Players.Count());
        var player = table.Players.First(x => x.Seat == new Seat(3));
        Assert.Equal(new Nickname("Charlie"), player.Nickname);
        Assert.Equal(new Seat(3), player.Seat);
        Assert.Equal(new Chips(1000), player.Stack);
        Assert.False(player.IsWaitingForBigBlind);
        Assert.False(player.IsDisconnected);
        Assert.False(player.IsSittingOut);

        Assert.Single(events);
        var @event = Assert.IsType<PlayerSatDownEvent>(events[0]);
        Assert.Equal(new Nickname("Charlie"), @event.Nickname);
        Assert.Equal(new Seat(3), @event.Seat);
        Assert.Equal(new Chips(1000), @event.Stack);
        Assert.False(@event.IsWaitingForBigBlind);
    }

    [Fact]
    public void SitDown_3rdPlayerStandard_ShouldSitDownAndWaitForBigBlind()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        );

        // Act
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(3),
            stack: new Chips(1000),
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(3, table.Players.Count());
        var player = table.Players.First(x => x.Seat == new Seat(3));
        Assert.Equal(new Nickname("Charlie"), player.Nickname);
        Assert.Equal(new Seat(3), player.Seat);
        Assert.Equal(new Chips(1000), player.Stack);
        Assert.True(player.IsWaitingForBigBlind);
        Assert.False(player.IsDisconnected);
        Assert.False(player.IsSittingOut);

        Assert.Single(events);
        var @event = Assert.IsType<PlayerSatDownEvent>(events[0]);
        Assert.Equal(new Nickname("Charlie"), @event.Nickname);
        Assert.Equal(new Seat(3), @event.Seat);
        Assert.Equal(new Chips(1000), @event.Stack);
        Assert.True(@event.IsWaitingForBigBlind);
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
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitDown(
                nickname: new Nickname("Alice"),
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
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitDown(
                nickname: new Nickname("Bob"),
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

        var table = CreateTable(maxSeat: 6);

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitDown(
                nickname: new Nickname("Alice"),
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
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        table.StandUp(
            nickname: new Nickname("Alice"),
            eventBus: eventBus
        );

        // Assert
        Assert.Empty(table.Players);

        Assert.Single(events);
        var @event = Assert.IsType<PlayerStoodUpEvent>(events[0]);
        Assert.Equal(new Nickname("Alice"), @event.Nickname);
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
                nickname: new Nickname("Alice"),
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
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        table.SitOut(
            nickname: new Nickname("Alice"),
            eventBus: eventBus
        );

        // Assert
        var player = table.Players.First();
        Assert.True(player.IsSittingOut);
        Assert.False(player.IsWaitingForBigBlind);

        Assert.Single(events);
        var @event = Assert.IsType<PlayerSatOutEvent>(events[0]);
        Assert.Equal(new Nickname("Alice"), @event.Nickname);
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
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitOut(
            nickname: new Nickname("Alice"),
            eventBus: new EventBus()
        );

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitOut(
                nickname: new Nickname("Alice"),
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
                nickname: new Nickname("Alice"),
                eventBus: eventBus
            );
        });

        // Assert
        Assert.Equal("A player with the given nickname is not found at the table", exc.Message);
        Assert.Empty(events);
    }

    [Fact]
    public void SitIn_1stPlayer_ShouldSitInAndNotWaitForBigBlind()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitOut(
            nickname: new Nickname("Alice"),
            eventBus: new EventBus()
        );

        // Act
        table.SitIn(
            nickname: new Nickname("Alice"),
            eventBus: eventBus
        );

        // Assert
        var player = table.Players.First();
        Assert.False(player.IsSittingOut);
        Assert.False(player.IsWaitingForBigBlind);

        Assert.Single(events);
        var @event = Assert.IsType<PlayerSatInEvent>(events[0]);
        Assert.Equal(new Nickname("Alice"), @event.Nickname);
        Assert.False(@event.IsWaitingForBigBlind);
    }

    [Fact]
    public void SitIn_2ndPlayer_ShouldSitInAndNotWaitForBigBlind()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitOut(
            nickname: new Nickname("Alice"),
            eventBus: new EventBus()
        );

        // Act
        table.SitIn(
            nickname: new Nickname("Alice"),
            eventBus: eventBus
        );

        // Assert
        var player = table.Players.First();
        Assert.False(player.IsSittingOut);
        Assert.False(player.IsWaitingForBigBlind);

        Assert.Single(events);
        var @event = Assert.IsType<PlayerSatInEvent>(events[0]);
        Assert.Equal(new Nickname("Alice"), @event.Nickname);
        Assert.False(@event.IsWaitingForBigBlind);
    }

    [Fact]
    public void SitIn_3rdPlayerInitial_ShouldSitInAndNotWaitForBigBlind()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(3),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitOut(
            nickname: new Nickname("Alice"),
            eventBus: new EventBus()
        );

        // Act
        table.SitIn(
            nickname: new Nickname("Alice"),
            eventBus: eventBus
        );

        // Assert
        var player = table.Players.First();
        Assert.False(player.IsSittingOut);
        Assert.False(player.IsWaitingForBigBlind);

        Assert.Single(events);
        var @event = Assert.IsType<PlayerSatInEvent>(events[0]);
        Assert.Equal(new Nickname("Alice"), @event.Nickname);
        Assert.False(@event.IsWaitingForBigBlind);
    }

    [Fact]
    public void SitIn_3rdPlayerStandard_ShouldSitInAndWaitForBigBlind()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(3),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitOut(
            nickname: new Nickname("Alice"),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        );

        // Act
        table.SitIn(
            nickname: new Nickname("Alice"),
            eventBus: eventBus
        );

        // Assert
        var player = table.Players.First();
        Assert.False(player.IsSittingOut);
        Assert.True(player.IsWaitingForBigBlind);

        Assert.Single(events);
        var @event = Assert.IsType<PlayerSatInEvent>(events[0]);
        Assert.Equal(new Nickname("Alice"), @event.Nickname);
        Assert.True(@event.IsWaitingForBigBlind);
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
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitIn(
                nickname: new Nickname("Alice"),
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
                nickname: new Nickname("Alice"),
                eventBus: eventBus
            );
        });

        // Assert
        Assert.Equal("A player with the given nickname is not found at the table", exc.Message);
        Assert.Empty(events);
    }

    [Fact]
    public void StartHand_HeadsUpInitial_ShouldAssignButton()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(2), table.ButtonSeat);
        Assert.Equal(new Seat(2), table.SmallBlindSeat);
        Assert.Equal(new Seat(4), table.BigBlindSeat);

        Assert.Single(events);
        var @event = Assert.IsType<HandIsStartedEvent>(events[0]);
        Assert.Equal(handUid, @event.HandUid);
    }

    [Fact]
    public void StartHand_HeadsUpStandard_ShouldRotateButton()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is BU/SB, Bob is BB

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(4), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);

        Assert.Single(events);
        var @event = Assert.IsType<HandIsStartedEvent>(events[0]);
        Assert.Equal(handUid, @event.HandUid);
    }

    [Fact]
    public void StartHand_HeadsUpPlayerSatDownNextToSmallBlind_ShouldWaitForBigBlind()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is SB/BU, Bob is BB
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(3),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(4), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);

        var player = table.Players.First(p => p.Seat == new Seat(3));
        Assert.True(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void StartHand_HeadsUpPlayerSatDownNextToBigBlind_ShouldSkipSmallBlind()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is SB/BU, Bob is BB
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Null(table.SmallBlindSeat); // No small blind when a heads-up table transformed into 3max
        Assert.Equal(new Seat(6), table.BigBlindSeat);

        var player = table.Players.First(p => p.Seat == new Seat(6));
        Assert.False(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void StartHand_HeadsUpFromDeadButton_ShouldKeepBigBlind()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is BU, Bob is SB, Charlie is BB
        table.StandUp(
            nickname: new Nickname("Bob"),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is BB, Bob's seat is dead button, Charlie is SB

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(6), table.ButtonSeat);
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat); // Posts 2nd BB in a row
    }

    [Fact]
    public void StartHand_3MaxInitial_ShouldAssignButton()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(2), table.ButtonSeat);
        Assert.Equal(new Seat(4), table.SmallBlindSeat);
        Assert.Equal(new Seat(6), table.BigBlindSeat);

        Assert.Single(events);
        var @event = Assert.IsType<HandIsStartedEvent>(events[0]);
        Assert.Equal(handUid, @event.HandUid);
    }

    [Fact]
    public void StartHand_3MaxStandard_ShouldRotateButton()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is BU, Bob is SB, Charlie is BB

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);

        Assert.Single(events);
        var @event = Assert.IsType<HandIsStartedEvent>(events[0]);
        Assert.Equal(handUid, @event.HandUid);
    }

    [Fact]
    public void StartHand_3MaxPlayerSatDownNextToButton_ShouldWaitForBigBlind()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is BU, Bob is SB, Charlie is BB
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(3),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);

        var player = table.Players.First(p => p.Seat == new Seat(3));
        Assert.True(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void StartHand_3MaxPlayerSatDownNextToSmallBlind_ShouldWaitForBigBlind()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is BU, Bob is SB, Charlie is BB
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(5),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);

        var player = table.Players.First(p => p.Seat == new Seat(5));
        Assert.True(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void StartHand_3MaxPlayerSatDownNextToBigBlind_ShouldRotateButton()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is BU, Bob is SB, Charlie is BB
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(7),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(7), table.BigBlindSeat);

        var player = table.Players.First(p => p.Seat == new Seat(7));
        Assert.False(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void StartHand_3MaxButtonStoodUp_ShouldKeepBigBlind()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is BU, Bob is SB, Charlie is BB
        table.StandUp(
            nickname: new Nickname("Alice"),
            eventBus: new EventBus()
        );

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(4), table.SmallBlindSeat);
        Assert.Equal(new Seat(6), table.BigBlindSeat); // Posts 2nd BB in a row
    }

    [Fact]
    public void StartHand_3MaxSmallBlindStoodUp_ShouldRotateButtonToEmptySeat()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is BU, Bob is SB, Charlie is BB
        table.StandUp(
            nickname: new Nickname("Bob"),
            eventBus: new EventBus()
        );

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(4), table.ButtonSeat); // Dead button
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);
    }

    [Fact]
    public void StartHand_3MaxBigBlindStoodUp_ShouldSkipSmallBlind()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is BU, Bob is SB, Charlie is BB
        table.StandUp(
            nickname: new Nickname("Charlie"),
            eventBus: new EventBus()
        );

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Null(table.SmallBlindSeat); // No small blind after big blind left the table
        Assert.Equal(new Seat(2), table.BigBlindSeat);
    }

    [Fact]
    public void StartHand_3MaxFromDeadButton_ShouldRotateButton()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(8),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is BU, Bob is SB, Charlie is BB, Diana is CO
        table.StandUp(
            nickname: new Nickname("Bob"),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is CO, Bob's seat is the dead button, Charlie is SB, Diana is BB

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(6), table.ButtonSeat);
        Assert.Equal(new Seat(8), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);
    }

    [Fact]
    public void StartHand_4MaxButtonStoodUp_ShouldRotateButton()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(8),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is BU, Bob is SB, Charlie is BB, Diana is CO
        table.StandUp(
            nickname: new Nickname("Alice"),
            eventBus: new EventBus()
        );

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(8), table.BigBlindSeat);
    }

    [Fact]
    public void StartHand_4MaxSmallBlindStoodUp_ShouldRotateButtonToEmptySeat()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(8),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is BU, Bob is SB, Charlie is BB, Diana is CO
        table.StandUp(
            nickname: new Nickname("Bob"),
            eventBus: new EventBus()
        );

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(4), table.ButtonSeat); // Dead button
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(8), table.BigBlindSeat);
    }

    [Fact]
    public void StartHand_4MaxBigBlindStoodUp_ShouldRotateButton()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(8),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is BU, Bob is SB, Charlie is BB, Diana is CO
        table.StandUp(
            nickname: new Nickname("Charlie"),
            eventBus: new EventBus()
        );

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Null(table.SmallBlindSeat); // No small blind after big blind left the table
        Assert.Equal(new Seat(8), table.BigBlindSeat);
    }

    [Fact]
    public void StartHand_4MaxCutOffStoodUp_ShouldRotateButton()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(4),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(8),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid()),
            eventBus: new EventBus()
        ); // Alice is BU, Bob is SB, Charlie is BB, Diana is CO
        table.StandUp(
            nickname: new Nickname("Diana"),
            eventBus: new EventBus()
        );

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid,
            eventBus: eventBus
        );

        // Assert
        Assert.Equal(handUid, table.HandUid);
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);
    }

    [Fact]
    public void StartHand_NotEnoughPlayers_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var events = new List<BaseEvent>();
        var listener = (BaseEvent e) => events.Add(e);
        var eventBus = new EventBus();
        eventBus.Subscribe(listener);

        var handUid = new HandUid(Guid.NewGuid());
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitDown(
            nickname: new Nickname("Bob"),
            seat: new Seat(1),
            stack: new Chips(1000),
            eventBus: new EventBus()
        );
        table.SitOut(
            nickname: new Nickname("Bob"),
            eventBus: new EventBus()
        );

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.StartHand(
                handUid: handUid,
                eventBus: eventBus
            );
        });

        // Assert
        Assert.Equal("The table does not have enough players to start a hand", exc.Message);
        Assert.Empty(events);
    }

    private Table CreateTable(
        int maxSeat = 9,
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

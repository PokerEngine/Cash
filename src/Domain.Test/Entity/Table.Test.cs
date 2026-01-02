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
        var uid = new TableUid(Guid.NewGuid());

        // Act
        var table = Table.FromScratch(
            uid: uid,
            game: Game.NoLimitHoldem,
            maxSeat: new Seat(maxSeat),
            smallBlind: new Chips(5),
            bigBlind: new Chips(10),
            chipCost: new Money(1, Currency.Usd)
        );

        // Assert
        Assert.Equal(uid, table.Uid);
        Assert.Equal(Game.NoLimitHoldem, table.Game);
        Assert.Equal(new Seat(maxSeat), table.MaxSeat);
        Assert.Equal(new Chips(5), table.SmallBlind);
        Assert.Equal(new Chips(10), table.BigBlind);
        Assert.Equal(new Money(1, Currency.Usd), table.ChipCost);
        Assert.Null(table.ButtonSeat);
        Assert.Null(table.SmallBlindSeat);
        Assert.Null(table.BigBlindSeat);
        Assert.Empty(table.Players);

        var events = table.PullEvents();
        Assert.Single(events);
        var @event = Assert.IsType<TableIsCreatedEvent>(events[0]);
        Assert.Equal(Game.NoLimitHoldem, @event.Game);
        Assert.Equal(new Chips(5), @event.SmallBlind);
        Assert.Equal(new Chips(10), @event.BigBlind);
        Assert.Equal(new Money(1, Currency.Usd), @event.ChipCost);
        Assert.Equal(new Seat(maxSeat), @event.MaxSeat);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(9)]
    public void FromEvents_Valid_ShouldCreate(int maxSeat)
    {
        // Arrange
        var uid = new TableUid(Guid.NewGuid());
        var handUid1 = new HandUid(Guid.NewGuid());
        var handUid2 = new HandUid(Guid.NewGuid());
        var events = new List<IEvent>
        {
            new TableIsCreatedEvent
            {
                Game = Game.NoLimitHoldem,
                MaxSeat = new Seat(maxSeat),
                SmallBlind = new Chips(5),
                BigBlind = new Chips(10),
                ChipCost = new Money(1, Currency.Usd),
                OccuredAt = DateTime.Now
            },
            new PlayerSatDownEvent
            {
                Nickname = new Nickname("Alice"),
                Seat = new Seat(2),
                Stack = new Chips(1000),
                OccuredAt = DateTime.Now
            },
            new PlayerSatDownEvent
            {
                Nickname = new Nickname("Bobby"),
                Seat = new Seat(4),
                Stack = new Chips(1000),
                OccuredAt = DateTime.Now
            },
            new ButtonIsRotatedEvent
            {
                OccuredAt = DateTime.Now
            },
            new HandIsStartedEvent
            {
                HandUid = handUid1,
                OccuredAt = DateTime.Now
            },
            new PlayerSatDownEvent
            {
                Nickname = new Nickname("Charlie"),
                Seat = new Seat(3),
                Stack = new Chips(1000),
                OccuredAt = DateTime.Now
            },
            new PlayerSatOutEvent
            {
                Nickname = new Nickname("Charlie"),
                OccuredAt = DateTime.Now
            },
            new PlayerSatDownEvent
            {
                Nickname = new Nickname("Diana"),
                Seat = new Seat(5),
                Stack = new Chips(1000),
                OccuredAt = DateTime.Now
            },
            new PlayerSatOutEvent
            {
                Nickname = new Nickname("Diana"),
                OccuredAt = DateTime.Now
            },
            new HandIsFinishedEvent
            {
                HandUid = handUid1,
                OccuredAt = DateTime.Now
            },
            new ButtonIsRotatedEvent
            {
                OccuredAt = DateTime.Now
            },
            new HandIsStartedEvent
            {
                HandUid = handUid2,
                OccuredAt = DateTime.Now
            },
            new PlayerSatInEvent
            {
                Nickname = new Nickname("Diana"),
                OccuredAt = DateTime.Now
            },
            new PlayerStoodUpEvent
            {
                Nickname = new Nickname("Bobby"),
                OccuredAt = DateTime.Now
            }
        };

        // Act
        var table = Table.FromEvents(uid, events);

        // Assert
        Assert.Equal(uid, table.Uid);
        Assert.Equal(Game.NoLimitHoldem, table.Game);
        Assert.Equal(new Chips(5), table.SmallBlind);
        Assert.Equal(new Chips(10), table.BigBlind);
        Assert.Equal(new Money(1, Currency.Usd), table.ChipCost);
        Assert.Equal(new Seat(maxSeat), table.MaxSeat);
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(4), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);
        Assert.Equal(handUid2, table.GetHandUid());
        Assert.Equal(3, table.Players.Count());
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void SitDown_1stPlayer_ShouldSitDownAndNotWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.PullEvents();

        // Act
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
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

        var events = table.PullEvents();
        Assert.Single(events);
        var @event = Assert.IsType<PlayerSatDownEvent>(events[0]);
        Assert.Equal(new Nickname("Alice"), @event.Nickname);
        Assert.Equal(new Seat(1), @event.Seat);
        Assert.Equal(new Chips(1000), @event.Stack);
    }

    [Fact]
    public void SitDown_2ndPlayer_ShouldSitDownAndNotWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );

        // Act
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );

        // Assert
        Assert.Equal(2, table.Players.Count());
        var player = table.Players.First(x => x.Seat == new Seat(2));
        Assert.Equal(new Nickname("Bobby"), player.Nickname);
        Assert.Equal(new Seat(2), player.Seat);
        Assert.Equal(new Chips(1000), player.Stack);
        Assert.False(player.IsWaitingForBigBlind);
        Assert.False(player.IsDisconnected);
        Assert.False(player.IsSittingOut);
    }

    [Fact]
    public void SitDown_3rdPlayerInitial_ShouldSitDownAndNotWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );

        // Act
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(3),
            stack: new Chips(1000)
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
    }

    [Fact]
    public void SitDown_3rdPlayerStandard_ShouldSitDownAndWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.RotateButton();
        table.StartHand(new HandUid(Guid.NewGuid()));

        // Act
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(3),
            stack: new Chips(1000)
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
    }

    [Fact]
    public void SitDown_IsSittingDown_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitDown(
                nickname: new Nickname("Alice"),
                seat: new Seat(2),
                stack: new Chips(1000)
            );
        });

        // Assert
        Assert.Equal("A player with the given nickname is already sitting down at the table", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void SitDown_SeatIsOccupied_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitDown(
                nickname: new Nickname("Bobby"),
                seat: new Seat(1),
                stack: new Chips(1000)
            );
        });

        // Assert
        Assert.Equal("The seat has already been occupied at the table", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void SitDown_SeatIsOutOfRange_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var table = CreateTable(maxSeat: 6);
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitDown(
                nickname: new Nickname("Alice"),
                seat: new Seat(7),
                stack: new Chips(1000)
            );
        });

        // Assert
        Assert.Equal("The table supports seats till #6", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void StandUp_IsSittingDown_ShouldStandUp()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        table.StandUp(
            nickname: new Nickname("Alice")
        );

        // Assert
        Assert.Empty(table.Players);

        var events = table.PullEvents();
        Assert.Single(events);
        var @event = Assert.IsType<PlayerStoodUpEvent>(events[0]);
        Assert.Equal(new Nickname("Alice"), @event.Nickname);
    }

    [Fact]
    public void StandUp_IsNotSittingDown_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var table = CreateTable();
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.StandUp(
                nickname: new Nickname("Alice")
            );
        });

        // Assert
        Assert.Equal("A player with the given nickname is not found at the table", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void SitOut_IsNotSittingOut_ShouldSitOut()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        table.SitOut(
            nickname: new Nickname("Alice")
        );

        // Assert
        var player = table.Players.First();
        Assert.True(player.IsSittingOut);
        Assert.False(player.IsWaitingForBigBlind);

        var events = table.PullEvents();
        Assert.Single(events);
        var @event = Assert.IsType<PlayerSatOutEvent>(events[0]);
        Assert.Equal(new Nickname("Alice"), @event.Nickname);
    }

    [Fact]
    public void SitOut_IsSittingOut_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.SitOut(
            nickname: new Nickname("Alice")
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitOut(
                nickname: new Nickname("Alice")
            );
        });

        // Assert
        Assert.Equal("The player is already sitting out", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void SitOut_IsNotSittingDown_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var table = CreateTable();
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitOut(
                nickname: new Nickname("Alice")
            );
        });

        // Assert
        Assert.Equal("A player with the given nickname is not found at the table", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void SitIn_1stPlayer_ShouldSitInAndNotWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.SitOut(
            nickname: new Nickname("Alice")
        );
        table.PullEvents();

        // Act
        table.SitIn(
            nickname: new Nickname("Alice")
        );

        // Assert
        var player = table.Players.First();
        Assert.False(player.IsSittingOut);
        Assert.False(player.IsWaitingForBigBlind);

        var events = table.PullEvents();
        Assert.Single(events);
        var @event = Assert.IsType<PlayerSatInEvent>(events[0]);
        Assert.Equal(new Nickname("Alice"), @event.Nickname);
    }

    [Fact]
    public void SitIn_2ndPlayer_ShouldSitInAndNotWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitOut(
            nickname: new Nickname("Alice")
        );

        // Act
        table.SitIn(
            nickname: new Nickname("Alice")
        );

        // Assert
        var player = table.Players.First();
        Assert.False(player.IsSittingOut);
        Assert.False(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void SitIn_3rdPlayerInitial_ShouldSitInAndNotWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(3),
            stack: new Chips(1000)
        );
        table.SitOut(
            nickname: new Nickname("Alice")
        );

        // Act
        table.SitIn(
            nickname: new Nickname("Alice")
        );

        // Assert
        var player = table.Players.First();
        Assert.False(player.IsSittingOut);
        Assert.False(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void SitIn_3rdPlayerStandard_ShouldSitInAndWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(3),
            stack: new Chips(1000)
        );
        table.SitOut(
            nickname: new Nickname("Alice")
        );
        table.RotateButton();
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );

        // Act
        table.SitIn(
            nickname: new Nickname("Alice")
        );

        // Assert
        var player = table.Players.First();
        Assert.False(player.IsSittingOut);
        Assert.True(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void SitIn_IsNotSittingOut_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitIn(
                nickname: new Nickname("Alice")
            );
        });

        // Assert
        Assert.Equal("The player is not sitting out yet", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void SitIn_IsNotSittingDown_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var table = CreateTable();
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.SitIn(
                nickname: new Nickname("Alice")
            );
        });

        // Assert
        Assert.Equal("A player with the given nickname is not found at the table", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void RotateButton_HeadsUpInitial_ShouldAssignButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(2), table.ButtonSeat);
        Assert.Equal(new Seat(2), table.SmallBlindSeat);
        Assert.Equal(new Seat(4), table.BigBlindSeat);

        var events = table.PullEvents();
        Assert.Single(events);
        Assert.IsType<ButtonIsRotatedEvent>(events[0]);
    }

    [Fact]
    public void RotateButton_HeadsUpStandard_ShouldRotateButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU/SB, Bobby is BB
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );
        table.PullEvents();

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(4), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);

        var events = table.PullEvents();
        Assert.Single(events);
        Assert.IsType<ButtonIsRotatedEvent>(events[0]);
    }

    [Fact]
    public void RotateButton_HeadsUpPlayerSatDownNextToSmallBlind_ShouldWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.RotateButton();  // Alice is SB/BU, Bobby is BB
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(3),
            stack: new Chips(1000)
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(4), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);

        var player = table.Players.First(p => p.Seat == new Seat(3));
        Assert.True(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void RotateButton_HeadsUpPlayerSatDownNextToBigBlind_ShouldSkipSmallBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is SB/BU, Bobby is BB
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Null(table.SmallBlindSeat); // No small blind when a heads-up table transformed into 3max
        Assert.Equal(new Seat(6), table.BigBlindSeat);

        var player = table.Players.First(p => p.Seat == new Seat(6));
        Assert.False(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void RotateButton_HeadsUpFromDeadButton_ShouldKeepBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.StandUp(
            nickname: new Nickname("Bobby")
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );
        table.RotateButton(); // Alice is BB, Bobby's seat is dead button, Charlie is SB
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(6), table.ButtonSeat);
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat); // Posts 2nd BB in a row
    }

    [Fact]
    public void RotateButton_3MaxInitial_ShouldAssignButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(2), table.ButtonSeat);
        Assert.Equal(new Seat(4), table.SmallBlindSeat);
        Assert.Equal(new Seat(6), table.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_3MaxStandard_ShouldRotateButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_3MaxPlayerSatDownNextToButton_ShouldWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(3),
            stack: new Chips(1000)
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);

        var player = table.Players.First(p => p.Seat == new Seat(3));
        Assert.True(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void RotateButton_3MaxPlayerSatDownNextToSmallBlind_ShouldWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(5),
            stack: new Chips(1000)
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);

        var player = table.Players.First(p => p.Seat == new Seat(5));
        Assert.True(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void RotateButton_3MaxPlayerSatDownNextToBigBlind_ShouldRotateButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(7),
            stack: new Chips(1000)
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(7), table.BigBlindSeat);

        var player = table.Players.First(p => p.Seat == new Seat(7));
        Assert.False(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void RotateButton_3MaxButtonStoodUp_ShouldKeepBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.StandUp(
            nickname: new Nickname("Alice")
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(4), table.SmallBlindSeat);
        Assert.Equal(new Seat(6), table.BigBlindSeat); // Posts 2nd BB in a row
    }

    [Fact]
    public void RotateButton_3MaxSmallBlindStoodUp_ShouldRotateButtonToEmptySeat()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.StandUp(
            nickname: new Nickname("Bobby")
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(4), table.ButtonSeat); // Dead button
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_3MaxBigBlindStoodUp_ShouldSkipSmallBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.StandUp(
            nickname: new Nickname("Charlie")
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Null(table.SmallBlindSeat); // No small blind after big blind left the table
        Assert.Equal(new Seat(2), table.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_3MaxFromDeadButton_ShouldRotateButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(8),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB, Diana is CO
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.StandUp(
            nickname: new Nickname("Bobby")
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );
        table.RotateButton(); // Alice is CO, Bobby's seat is the dead button, Charlie is SB, Diana is BB
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(6), table.ButtonSeat);
        Assert.Equal(new Seat(8), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_4MaxButtonStoodUp_ShouldRotateButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(8),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB, Diana is CO
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.StandUp(
            nickname: new Nickname("Alice")
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(8), table.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_4MaxSmallBlindStoodUp_ShouldRotateButtonToEmptySeat()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(8),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB, Diana is CO
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.StandUp(
            nickname: new Nickname("Bobby")
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(4), table.ButtonSeat); // Dead button
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(8), table.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_4MaxBigBlindStoodUp_ShouldRotateButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(8),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB, Diana is CO
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.StandUp(
            nickname: new Nickname("Charlie")
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Null(table.SmallBlindSeat); // No small blind after big blind left the table
        Assert.Equal(new Seat(8), table.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_4MaxCutOffStoodUp_ShouldRotateButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(8),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB, Diana is CO
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.StandUp(
            nickname: new Nickname("Diana")
        );
        table.FinishHand(
            handUid: table.GetHandUid()
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.Equal(new Seat(4), table.ButtonSeat);
        Assert.Equal(new Seat(6), table.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_NotEnoughPlayers_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitOut(
            nickname: new Nickname("Bobby")
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.RotateButton();
        });

        // Assert
        Assert.Equal("Not enough players to rotate the button", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void RotateButton_HandIsInProgress_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.RotateButton();
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.RotateButton();
        });

        // Assert
        Assert.Equal("The previous hand has not been finished yet", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void StartHand_Valid_ShouldStartHand()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.RotateButton();
        table.PullEvents();

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartHand(
            handUid: handUid
        );

        // Assert
        Assert.Equal(handUid, table.GetHandUid());
        Assert.True(table.IsHandInProgress());

        var events = table.PullEvents();
        Assert.Single(events);
        var @event = Assert.IsType<HandIsStartedEvent>(events[0]);
        Assert.Equal(handUid, @event.HandUid);
    }

    [Fact]
    public void StartHand_ButtonIsNotRotated_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.StartHand(
                handUid: new HandUid(Guid.NewGuid())
            );
        });

        // Assert
        Assert.Equal("The button must be rotated before starting a hand", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void StartHand_PreviousNotFinished_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.RotateButton();
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.StartHand(
                handUid: new HandUid(Guid.NewGuid())
            );
        });

        // Assert
        Assert.Equal("The previous hand has not been finished yet", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void StartHand_NotEnoughPlayers_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var handUid = new HandUid(Guid.NewGuid());
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.StartHand(
                handUid: handUid
            );
        });

        // Assert
        Assert.Equal("Not enough players to start a hand", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void FinishHand_Valid_ShouldFinishHand()
    {
        // Arrange
        var handUid = new HandUid(Guid.NewGuid());
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.RotateButton();
        table.StartHand(
            handUid: handUid
        );
        table.PullEvents();

        // Act
        table.FinishHand(
            handUid: handUid
        );

        // Assert
        Assert.False(table.IsHandInProgress());

        var events = table.PullEvents();
        Assert.Single(events);
        var @event = Assert.IsType<HandIsFinishedEvent>(events[0]);
        Assert.Equal(handUid, @event.HandUid);
    }

    [Fact]
    public void FinishHand_NotStarted_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.FinishHand(
                handUid: new HandUid(Guid.NewGuid())
            );
        });

        // Assert
        Assert.Equal("The hand has not been started yet", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void FinishHand_DifferentUid_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var table = CreateTable();
        table.SitDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.RotateButton();
        table.StartHand(
            handUid: new HandUid(Guid.NewGuid())
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidOperationException>(() =>
        {
            table.FinishHand(
                handUid: new HandUid(Guid.NewGuid())
            );
        });

        // Assert
        Assert.Equal("The hand does not match the current one", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    private Table CreateTable(
        int maxSeat = 9,
        int smallBlind = 5,
        int bigBlind = 10,
        decimal chipCost = 1
    )
    {
        return Table.FromScratch(
            uid: new TableUid(Guid.NewGuid()),
            game: Game.NoLimitHoldem,
            smallBlind: new Chips(smallBlind),
            bigBlind: new Chips(bigBlind),
            maxSeat: new Seat(maxSeat),
            chipCost: new Money(chipCost, Currency.Usd)
        );
    }
}

using Domain.Entity;
using Domain.Event;
using Domain.Exception;
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
            rules: new Rules
            {
                Game = Game.NoLimitHoldem,
                MaxSeat = new Seat(maxSeat),
                SmallBlind = new Chips(5),
                BigBlind = new Chips(10),
                ChipCost = new Money(1, Currency.Usd)
            }
        );

        // Assert
        Assert.Equal(uid, table.Uid);
        Assert.Equal(Game.NoLimitHoldem, table.Rules.Game);
        Assert.Equal(new Seat(maxSeat), table.Rules.MaxSeat);
        Assert.Equal(new Chips(5), table.Rules.SmallBlind);
        Assert.Equal(new Chips(10), table.Rules.BigBlind);
        Assert.Equal(new Money(1, Currency.Usd), table.Rules.ChipCost);
        Assert.Null(table.Positions);
        Assert.Empty(table.Players);

        var events = table.PullEvents();
        Assert.Single(events);
        var @event = Assert.IsType<TableCreatedEvent>(events[0]);
        Assert.Equal(Game.NoLimitHoldem, @event.Rules.Game);
        Assert.Equal(new Seat(maxSeat), @event.Rules.MaxSeat);
        Assert.Equal(new Chips(5), @event.Rules.SmallBlind);
        Assert.Equal(new Chips(10), @event.Rules.BigBlind);
        Assert.Equal(new Money(1, Currency.Usd), @event.Rules.ChipCost);
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
            new TableCreatedEvent
            {
                Rules = new Rules
                {
                    Game = Game.NoLimitHoldem,
                    MaxSeat = new Seat(maxSeat),
                    SmallBlind = new Chips(5),
                    BigBlind = new Chips(10),
                    ChipCost = new Money(1, Currency.Usd)
                },
                OccurredAt = DateTime.Now
            },
            new PlayerSatDownEvent
            {
                Nickname = new Nickname("Alice"),
                Seat = new Seat(2),
                Stack = new Chips(1000),
                OccurredAt = DateTime.Now
            },
            new PlayerSatDownEvent
            {
                Nickname = new Nickname("Bobby"),
                Seat = new Seat(4),
                Stack = new Chips(1000),
                OccurredAt = DateTime.Now
            },
            new ButtonRotatedEvent
            {
                OccurredAt = DateTime.Now
            },
            new CurrentHandStartedEvent
            {
                HandUid = handUid1,
                OccurredAt = DateTime.Now
            },
            new PlayerChipsDebitedEvent
            {
                Nickname = new Nickname("Alice"),
                Amount = new Chips(5),
                OccurredAt = DateTime.Now
            },
            new PlayerChipsDebitedEvent
            {
                Nickname = new Nickname("Bobby"),
                Amount = new Chips(10),
                OccurredAt = DateTime.Now
            },
            new PlayerChipsDebitedEvent
            {
                Nickname = new Nickname("Alice"),
                Amount = new Chips(20),
                OccurredAt = DateTime.Now
            },
            new PlayerSatDownEvent
            {
                Nickname = new Nickname("Charlie"),
                Seat = new Seat(3),
                Stack = new Chips(1000),
                OccurredAt = DateTime.Now
            },
            new PlayerSatOutEvent
            {
                Nickname = new Nickname("Charlie"),
                OccurredAt = DateTime.Now
            },
            new PlayerSatDownEvent
            {
                Nickname = new Nickname("Diana"),
                Seat = new Seat(5),
                Stack = new Chips(1000),
                OccurredAt = DateTime.Now
            },
            new PlayerSatOutEvent
            {
                Nickname = new Nickname("Diana"),
                OccurredAt = DateTime.Now
            },
            new PlayerChipsCreditedEvent
            {
                Nickname = new Nickname("Alice"),
                Amount = new Chips(35),
                OccurredAt = DateTime.Now
            },
            new CurrentHandFinishedEvent
            {
                HandUid = handUid1,
                OccurredAt = DateTime.Now
            },
            new ButtonRotatedEvent
            {
                OccurredAt = DateTime.Now
            },
            new CurrentHandStartedEvent
            {
                HandUid = handUid2,
                OccurredAt = DateTime.Now
            },
            new PlayerSatInEvent
            {
                Nickname = new Nickname("Diana"),
                OccurredAt = DateTime.Now
            },
            new PlayerStoodUpEvent
            {
                Nickname = new Nickname("Bobby"),
                OccurredAt = DateTime.Now
            }
        };

        // Act
        var table = Table.FromEvents(uid, events);

        // Assert
        Assert.Equal(uid, table.Uid);
        Assert.Equal(Game.NoLimitHoldem, table.Rules.Game);
        Assert.Equal(new Seat(maxSeat), table.Rules.MaxSeat);
        Assert.Equal(new Chips(5), table.Rules.SmallBlind);
        Assert.Equal(new Chips(10), table.Rules.BigBlind);
        Assert.Equal(new Money(1, Currency.Usd), table.Rules.ChipCost);
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(4), table.Positions.ButtonSeat);
        Assert.Equal(new Seat(4), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.Positions.BigBlindSeat);
        Assert.Equal(handUid2, table.GetCurrentHandUid());
        Assert.Equal(3, table.Players.Count());
        var alice = table.Players.First(p => p.Nickname == new Nickname("Alice"));
        Assert.Equal(new Chips(1010), alice.Stack);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void SitPlayerDown_1stPlayer_ShouldSitPlayerDownAndNotWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.PullEvents();

        // Act
        table.SitPlayerDown(
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
        Assert.False(player.IsSittingOut);

        var events = table.PullEvents();
        Assert.Single(events);
        var @event = Assert.IsType<PlayerSatDownEvent>(events[0]);
        Assert.Equal(new Nickname("Alice"), @event.Nickname);
        Assert.Equal(new Seat(1), @event.Seat);
        Assert.Equal(new Chips(1000), @event.Stack);
    }

    [Fact]
    public void SitPlayerDown_2ndPlayer_ShouldSitPlayerDownAndNotWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );

        // Act
        table.SitPlayerDown(
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
        Assert.False(player.IsSittingOut);
    }

    [Fact]
    public void SitPlayerDown_3rdPlayerInitial_ShouldSitPlayerDownAndNotWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );

        // Act
        table.SitPlayerDown(
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
        Assert.False(player.IsSittingOut);
    }

    [Fact]
    public void SitPlayerDown_3rdPlayerStandard_ShouldSitPlayerDownAndWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.RotateButton();
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));

        // Act
        table.SitPlayerDown(
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
        Assert.False(player.IsSittingOut);
    }

    [Fact]
    public void SitPlayerDown_IsSittingDown_ShouldThrowException()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<PlayerSatDownException>(() =>
        {
            table.SitPlayerDown(
                nickname: new Nickname("Alice"),
                seat: new Seat(2),
                stack: new Chips(1000)
            );
        });

        // Assert
        Assert.Equal("The player has already sat down at the table", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void SitPlayerDown_SeatIsOccupied_ShouldThrowException()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<SeatOccupiedException>(() =>
        {
            table.SitPlayerDown(
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
    public void SitPlayerDown_SeatIsOutOfRange_ShouldThrowException()
    {
        // Arrange
        var table = CreateTable(maxSeat: 6);
        table.PullEvents();

        // Act
        var exc = Assert.Throws<SeatNotFoundException>(() =>
        {
            table.SitPlayerDown(
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
    public void StandPlayerUp_IsSittingDown_ShouldStandPlayerUp()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        table.StandPlayerUp(new Nickname("Alice"));

        // Assert
        Assert.Empty(table.Players);

        var events = table.PullEvents();
        Assert.Single(events);
        var @event = Assert.IsType<PlayerStoodUpEvent>(events[0]);
        Assert.Equal(new Nickname("Alice"), @event.Nickname);
    }

    [Fact]
    public void StandPlayerUp_IsNotSittingDown_ShouldThrowException()
    {
        // Arrange
        var table = CreateTable();
        table.PullEvents();

        // Act
        var exc = Assert.Throws<PlayerNotFoundException>(() =>
        {
            table.StandPlayerUp(new Nickname("Alice"));
        });

        // Assert
        Assert.Equal("The player is not found at the table", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void SitPlayerOut_IsNotSittingOut_ShouldSitPlayerOut()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        table.SitPlayerOut(new Nickname("Alice"));

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
    public void SitPlayerOut_IsSittingOut_ShouldThrowException()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.SitPlayerOut(new Nickname("Alice"));
        table.PullEvents();

        // Act
        var exc = Assert.Throws<PlayerSatOutException>(() =>
        {
            table.SitPlayerOut(new Nickname("Alice"));
        });

        // Assert
        Assert.Equal("The player is already sitting out", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void SitPlayerOut_IsNotSittingDown_ShouldThrowException()
    {
        // Arrange
        var table = CreateTable();
        table.PullEvents();

        // Act
        var exc = Assert.Throws<PlayerNotFoundException>(() =>
        {
            table.SitPlayerOut(new Nickname("Alice"));
        });

        // Assert
        Assert.Equal("The player is not found at the table", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void SitPlayerIn_1stPlayer_ShouldSitPlayerInAndNotWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.SitPlayerOut(new Nickname("Alice"));
        table.PullEvents();

        // Act
        table.SitPlayerIn(new Nickname("Alice"));

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
    public void SitPlayerIn_2ndPlayer_ShouldSitPlayerInAndNotWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerOut(new Nickname("Alice"));

        // Act
        table.SitPlayerIn(new Nickname("Alice"));

        // Assert
        var player = table.Players.First();
        Assert.False(player.IsSittingOut);
        Assert.False(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void SitPlayerIn_3rdPlayerInitial_ShouldSitPlayerInAndNotWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(3),
            stack: new Chips(1000)
        );
        table.SitPlayerOut(new Nickname("Alice"));

        // Act
        table.SitPlayerIn(new Nickname("Alice"));

        // Assert
        var player = table.Players.First();
        Assert.False(player.IsSittingOut);
        Assert.False(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void SitPlayerIn_3rdPlayerStandard_ShouldSitPlayerInAndWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(3),
            stack: new Chips(1000)
        );
        table.SitPlayerOut(new Nickname("Alice"));
        table.RotateButton();
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));

        // Act
        table.SitPlayerIn(new Nickname("Alice"));

        // Assert
        var player = table.Players.First();
        Assert.False(player.IsSittingOut);
        Assert.True(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void SitPlayerIn_IsNotSittingOut_ShouldThrowException()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<PlayerNotSatOutException>(() =>
        {
            table.SitPlayerIn(new Nickname("Alice"));
        });

        // Assert
        Assert.Equal("The player is not sitting out yet", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void SitPlayerIn_IsNotSittingDown_ShouldThrowException()
    {
        // Arrange
        var table = CreateTable();
        table.PullEvents();

        // Act
        var exc = Assert.Throws<PlayerNotFoundException>(() =>
        {
            table.SitPlayerIn(new Nickname("Alice"));
        });

        // Assert
        Assert.Equal("The player is not found at the table", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void DebitPlayerChips_WhenEnoughStack_ShouldDebitPlayerChips()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        table.DebitPlayerChips(new Nickname("Alice"), new Chips(5));

        // Assert
        var player = table.Players.First(p => p.Nickname == new Nickname("Alice"));
        Assert.Equal(new Chips(995), player.Stack);
    }

    [Fact]
    public void DebitPlayerChips_WhenNotEnoughStack_ShouldThrowException()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InsufficientChipsException>(() =>
        {
            table.DebitPlayerChips(new Nickname("Alice"), new Chips(1001));
        });

        // Assert
        Assert.Equal("Cannot subtract more chips than available", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void CreditPlayerChips_WhenNotAllIn_ShouldCreditPlayerChips()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        table.CreditPlayerChips(new Nickname("Alice"), new Chips(100));

        // Assert
        var player = table.Players.First(p => p.Nickname == new Nickname("Alice"));
        Assert.Equal(new Chips(1100), player.Stack);
    }

    [Fact]
    public void CreditPlayerChips_WhenAllIn_ShouldCreditPlayerChips()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(1),
            stack: new Chips(0)
        );
        table.PullEvents();

        // Act
        table.CreditPlayerChips(new Nickname("Alice"), new Chips(100));

        // Assert
        var player = table.Players.First(p => p.Nickname == new Nickname("Alice"));
        Assert.Equal(new Chips(100), player.Stack);
    }

    [Fact]
    public void RotateButton_HeadsUpInitial_ShouldAssignButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(2), table.Positions.ButtonSeat);
        Assert.Equal(new Seat(2), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(4), table.Positions.BigBlindSeat);

        var events = table.PullEvents();
        Assert.Single(events);
        Assert.IsType<ButtonRotatedEvent>(events[0]);
    }

    [Fact]
    public void RotateButton_HeadsUpStandard_ShouldRotateButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU/SB, Bobby is BB
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.FinishCurrentHand(table.GetCurrentHandUid());
        table.PullEvents();

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(4), table.Positions.ButtonSeat);
        Assert.Equal(new Seat(4), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.Positions.BigBlindSeat);

        var events = table.PullEvents();
        Assert.Single(events);
        Assert.IsType<ButtonRotatedEvent>(events[0]);
    }

    [Fact]
    public void RotateButton_HeadsUpPlayerSatDownNextToSmallBlind_ShouldWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.RotateButton();  // Alice is SB/BU, Bobby is BB
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(3),
            stack: new Chips(1000)
        );
        table.FinishCurrentHand(table.GetCurrentHandUid());

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(4), table.Positions.ButtonSeat);
        Assert.Equal(new Seat(4), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.Positions.BigBlindSeat);

        var player = table.Players.First(p => p.Seat == new Seat(3));
        Assert.True(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void RotateButton_HeadsUpPlayerSatDownNextToBigBlind_ShouldSkipSmallBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is SB/BU, Bobby is BB
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.FinishCurrentHand(table.GetCurrentHandUid());

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(4), table.Positions.ButtonSeat);
        Assert.Null(table.Positions.SmallBlindSeat); // No small blind when a heads-up table transformed into 3max
        Assert.Equal(new Seat(6), table.Positions.BigBlindSeat);

        var player = table.Players.First(p => p.Seat == new Seat(6));
        Assert.False(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void RotateButton_HeadsUpFromDeadButton_ShouldKeepBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.StandPlayerUp(new Nickname("Bobby"));
        table.FinishCurrentHand(table.GetCurrentHandUid());
        table.RotateButton(); // Alice is BB, Bobby's seat is dead button, Charlie is SB
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.FinishCurrentHand(table.GetCurrentHandUid());

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(6), table.Positions.ButtonSeat);
        Assert.Equal(new Seat(6), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.Positions.BigBlindSeat); // Posts 2nd BB in a row
    }

    [Fact]
    public void RotateButton_3MaxInitial_ShouldAssignButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(2), table.Positions.ButtonSeat);
        Assert.Equal(new Seat(4), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(6), table.Positions.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_3MaxStandard_ShouldRotateButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.FinishCurrentHand(table.GetCurrentHandUid());

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(4), table.Positions.ButtonSeat);
        Assert.Equal(new Seat(6), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.Positions.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_3MaxPlayerSatDownNextToButton_ShouldWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.SitPlayerDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(3),
            stack: new Chips(1000)
        );
        table.FinishCurrentHand(table.GetCurrentHandUid());

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(4), table.Positions.ButtonSeat);
        Assert.Equal(new Seat(6), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.Positions.BigBlindSeat);

        var player = table.Players.First(p => p.Seat == new Seat(3));
        Assert.True(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void RotateButton_3MaxPlayerSatDownNextToSmallBlind_ShouldWaitForBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.SitPlayerDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(5),
            stack: new Chips(1000)
        );
        table.FinishCurrentHand(table.GetCurrentHandUid());

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(4), table.Positions.ButtonSeat);
        Assert.Equal(new Seat(6), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.Positions.BigBlindSeat);

        var player = table.Players.First(p => p.Seat == new Seat(5));
        Assert.True(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void RotateButton_3MaxPlayerSatDownNextToBigBlind_ShouldRotateButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.SitPlayerDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(7),
            stack: new Chips(1000)
        );
        table.FinishCurrentHand(table.GetCurrentHandUid());

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(4), table.Positions.ButtonSeat);
        Assert.Equal(new Seat(6), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(7), table.Positions.BigBlindSeat);

        var player = table.Players.First(p => p.Seat == new Seat(7));
        Assert.False(player.IsWaitingForBigBlind);
    }

    [Fact]
    public void RotateButton_3MaxButtonStoodUp_ShouldKeepBigBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.StandPlayerUp(new Nickname("Alice"));
        table.FinishCurrentHand(table.GetCurrentHandUid());

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(4), table.Positions.ButtonSeat);
        Assert.Equal(new Seat(4), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(6), table.Positions.BigBlindSeat); // Posts 2nd BB in a row
    }

    [Fact]
    public void RotateButton_3MaxSmallBlindStoodUp_ShouldRotateButtonToEmptySeat()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.StandPlayerUp(new Nickname("Bobby"));
        table.FinishCurrentHand(table.GetCurrentHandUid());

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(4), table.Positions.ButtonSeat); // Dead button
        Assert.Equal(new Seat(6), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.Positions.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_3MaxBigBlindStoodUp_ShouldSkipSmallBlind()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.StandPlayerUp(new Nickname("Charlie"));
        table.FinishCurrentHand(table.GetCurrentHandUid());

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(4), table.Positions.ButtonSeat);
        Assert.Null(table.Positions.SmallBlindSeat); // No small blind after big blind left the table
        Assert.Equal(new Seat(2), table.Positions.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_3MaxFromDeadButton_ShouldRotateButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(8),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB, Diana is CO
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.StandPlayerUp(new Nickname("Bobby"));
        table.FinishCurrentHand(table.GetCurrentHandUid());
        table.RotateButton(); // Alice is CO, Bobby's seat is the dead button, Charlie is SB, Diana is BB
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.FinishCurrentHand(table.GetCurrentHandUid());

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(6), table.Positions.ButtonSeat);
        Assert.Equal(new Seat(8), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.Positions.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_4MaxButtonStoodUp_ShouldRotateButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(8),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB, Diana is CO
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.StandPlayerUp(new Nickname("Alice"));
        table.FinishCurrentHand(table.GetCurrentHandUid());

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(4), table.Positions.ButtonSeat);
        Assert.Equal(new Seat(6), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(8), table.Positions.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_4MaxSmallBlindStoodUp_ShouldRotateButtonToEmptySeat()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(8),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB, Diana is CO
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.StandPlayerUp(new Nickname("Bobby"));
        table.FinishCurrentHand(table.GetCurrentHandUid());

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(4), table.Positions.ButtonSeat); // Dead button
        Assert.Equal(new Seat(6), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(8), table.Positions.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_4MaxBigBlindStoodUp_ShouldRotateButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(8),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB, Diana is CO
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.StandPlayerUp(new Nickname("Charlie"));
        table.FinishCurrentHand(table.GetCurrentHandUid());

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(4), table.Positions.ButtonSeat);
        Assert.Null(table.Positions.SmallBlindSeat); // No small blind after big blind left the table
        Assert.Equal(new Seat(8), table.Positions.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_4MaxCutOffStoodUp_ShouldRotateButton()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Charlie"),
            seat: new Seat(6),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Diana"),
            seat: new Seat(8),
            stack: new Chips(1000)
        );
        table.RotateButton(); // Alice is BU, Bobby is SB, Charlie is BB, Diana is CO
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.StandPlayerUp(
            nickname: new Nickname("Diana")
        );
        table.FinishCurrentHand(table.GetCurrentHandUid());

        // Act
        table.RotateButton();

        // Assert
        Assert.NotNull(table.Positions);
        Assert.Equal(new Seat(4), table.Positions.ButtonSeat);
        Assert.Equal(new Seat(6), table.Positions.SmallBlindSeat);
        Assert.Equal(new Seat(2), table.Positions.BigBlindSeat);
    }

    [Fact]
    public void RotateButton_NotEnoughPlayers_ShouldThrowException()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.SitPlayerOut(new Nickname("Bobby"));
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidTableStateException>(() =>
        {
            table.RotateButton();
        });

        // Assert
        Assert.Equal("Not enough players to rotate the button", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void RotateButton_HandIsInProgress_ShouldThrowException()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.RotateButton();
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidTableStateException>(() =>
        {
            table.RotateButton();
        });

        // Assert
        Assert.Equal("The previous hand has not been finished yet", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void StartCurrentHand_Valid_ShouldStartCurrentHand()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(4),
            stack: new Chips(1000)
        );
        table.RotateButton();
        table.PullEvents();

        // Act
        var handUid = new HandUid(Guid.NewGuid());
        table.StartCurrentHand(
            handUid: handUid
        );

        // Assert
        Assert.Equal(handUid, table.GetCurrentHandUid());
        Assert.True(table.IsHandInProgress());

        var events = table.PullEvents();
        Assert.Single(events);
        var @event = Assert.IsType<CurrentHandStartedEvent>(events[0]);
        Assert.Equal(handUid, @event.HandUid);
    }

    [Fact]
    public void StartCurrentHand_PreviousNotFinished_ShouldThrowException()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.RotateButton();
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidTableStateException>(() =>
        {
            table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        });

        // Assert
        Assert.Equal("The previous hand has not been finished yet", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void FinishCurrentHand_Valid_ShouldFinishCurrentHand()
    {
        // Arrange
        var handUid = new HandUid(Guid.NewGuid());
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.RotateButton();
        table.StartCurrentHand(
            handUid: handUid
        );
        table.PullEvents();

        // Act
        table.FinishCurrentHand(
            handUid: handUid
        );

        // Assert
        Assert.False(table.IsHandInProgress());

        var events = table.PullEvents();
        Assert.Single(events);
        var @event = Assert.IsType<CurrentHandFinishedEvent>(events[0]);
        Assert.Equal(handUid, @event.HandUid);
    }

    [Fact]
    public void FinishCurrentHand_NotStarted_ShouldThrowException()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidTableStateException>(() =>
        {
            table.FinishCurrentHand(
                handUid: new HandUid(Guid.NewGuid())
            );
        });

        // Assert
        Assert.Equal("The current hand has not been started yet", exc.Message);
        Assert.Empty(table.PullEvents());
    }

    [Fact]
    public void FinishCurrentHand_DifferentUid_ShouldThrowException()
    {
        // Arrange
        var table = CreateTable();
        table.SitPlayerDown(
            nickname: new Nickname("Alice"),
            seat: new Seat(2),
            stack: new Chips(1000)
        );
        table.SitPlayerDown(
            nickname: new Nickname("Bobby"),
            seat: new Seat(1),
            stack: new Chips(1000)
        );
        table.RotateButton();
        table.StartCurrentHand(new HandUid(Guid.NewGuid()));
        table.PullEvents();

        // Act
        var exc = Assert.Throws<InvalidTableStateException>(() =>
        {
            table.FinishCurrentHand(
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
            rules: new Rules
            {
                Game = Game.NoLimitHoldem,
                MaxSeat = new Seat(maxSeat),
                SmallBlind = new Chips(smallBlind),
                BigBlind = new Chips(bigBlind),
                ChipCost = new Money(chipCost, Currency.Usd)
            }
        );
    }
}

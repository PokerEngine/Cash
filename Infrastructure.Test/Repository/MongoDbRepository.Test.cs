using Application.Exception;
using Application.Repository;
using Domain.Event;
using Domain.ValueObject;
using Infrastructure.Repository;
using Infrastructure.Test.Client.MongoDb;
using Microsoft.Extensions.Options;

namespace Infrastructure.Test.Repository;

[Trait("Category", "Integration")]
public class MongoDbRepositoryTest(MongoDbClientFixture fixture) : IClassFixture<MongoDbClientFixture>
{
    [Fact]
    public async Task GetEventsAsync_WhenAdded_ShouldExtractEvents()
    {
        // Arrange
        var repository = CreateRepository();

        var tableUid = new TableUid(Guid.NewGuid());
        var @event = new TestEvent
        {
            Game = Game.NoLimitHoldem,
            Nickname = new Nickname("Alice"),
            Seat = new Seat(2),
            Chips = new Chips(1000),
            Money = new Money(12.34m, Currency.Usd),
            HandUid = Guid.NewGuid(),
            Rules = new Rules
            {
                Game = Game.NoLimitHoldem,
                MaxSeat = new Seat(6),
                SmallBlind = new Chips(5),
                BigBlind = new Chips(10),
                ChipCost = new Money(1, Currency.Usd)
            },
            Positions = new Positions
            {
                SmallBlindSeat = null,
                BigBlindSeat = new Seat(2),
                ButtonSeat = new Seat(6)
            },
            OccurredAt = GetNow()
        };
        await repository.AddEventsAsync(tableUid, [@event]);

        // Act
        var events = await repository.GetEventsAsync(tableUid);

        // Assert
        Assert.Single(events);
        var typedEvent = Assert.IsType<TestEvent>(events[0]);
        Assert.Equal(@event, typedEvent);
    }

    [Fact]
    public async Task GetEventsAsync_WhenNotAdded_ShouldThrowException()
    {
        // Arrange
        var repository = CreateRepository();

        var tableUid = new TableUid(Guid.NewGuid());
        var @event = new TestEvent
        {
            Game = Game.NoLimitHoldem,
            Nickname = new Nickname("Alice"),
            Seat = new Seat(2),
            Chips = new Chips(1000),
            Money = new Money(12.34m, Currency.Usd),
            HandUid = Guid.NewGuid(),
            Rules = new Rules
            {
                Game = Game.NoLimitHoldem,
                MaxSeat = new Seat(6),
                SmallBlind = new Chips(5),
                BigBlind = new Chips(10),
                ChipCost = new Money(1, Currency.Usd)
            },
            Positions = new Positions
            {
                SmallBlindSeat = null,
                BigBlindSeat = new Seat(2),
                ButtonSeat = new Seat(6)
            },
            OccurredAt = GetNow()
        };
        await repository.AddEventsAsync(tableUid, [@event]);

        // Act & Assert
        var exc = await Assert.ThrowsAsync<TableNotFoundException>(
            async () => await repository.GetEventsAsync(new TableUid(Guid.NewGuid()))
        );
        Assert.Equal("The table is not found", exc.Message);
    }

    private IRepository CreateRepository()
    {
        var client = fixture.CreateClient();
        var options = CreateOptions();
        return new MongoDbRepository(client, options);
    }

    private IOptions<MongoDbRepositoryOptions> CreateOptions()
    {
        var options = new MongoDbRepositoryOptions
        {
            Database = $"test_repository_{Guid.NewGuid()}"
        };
        return Options.Create(options);
    }

    private static DateTime GetNow()
    {
        // We truncate nanoseconds because they are not supported in Mongo
        var now = DateTime.Now;
        return new DateTime(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            now.Minute,
            now.Second,
            now.Millisecond,
            now.Kind
        );
    }
}

internal record TestEvent : IEvent
{
    public required Game Game { get; init; }
    public required Nickname Nickname { get; init; }
    public required Seat Seat { get; init; }
    public required Chips Chips { get; init; }
    public required Money Money { get; init; }
    public required HandUid HandUid { get; init; }
    public required Rules Rules { get; init; }
    public required Positions Positions { get; init; }
    public required DateTime OccurredAt { get; init; }
}

using Application.Repository;
using Domain.Event;
using Domain.ValueObject;
using Infrastructure.Repository;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Test.Repository;

[Trait("Category", "Integration")]
public class MongoDbRepositoryTest(MongoDbFixture fixture) : IClassFixture<MongoDbFixture>
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
            Nickname = new Nickname("alice"),
            Seat = new Seat(2),
            Chips = new Chips(1000),
            Money = new Money(12.34m, Currency.Usd),
            HandUid = Guid.NewGuid(),
            OccuredAt = GetNow()
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
    public async Task GetEventsAsync_WhenNotAdded_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var repository = CreateRepository();

        var tableUid = new TableUid(Guid.NewGuid());
        var @event = new TestEvent
        {
            Game = Game.NoLimitHoldem,
            Nickname = new Nickname("alice"),
            Seat = new Seat(2),
            Chips = new Chips(1000),
            Money = new Money(12.34m, Currency.Usd),
            HandUid = Guid.NewGuid(),
            OccuredAt = GetNow()
        };
        await repository.AddEventsAsync(tableUid, [@event]);

        // Act & Assert
        var exc = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await repository.GetEventsAsync(new TableUid(Guid.NewGuid()))
        );
        Assert.Equal("The table is not found", exc.Message);
    }

    private IRepository CreateRepository()
    {
        var options = Options.Create(fixture.CreateOptions());
        return new MongoDbRepository(
            options,
            NullLogger<MongoDbRepository>.Instance
        );
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
    public required DateTime OccuredAt { get; init; }
}

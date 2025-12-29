using Application.IntegrationEvent;
using Infrastructure.IntegrationEvent;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Infrastructure.Test.IntegrationEvent;

[Trait("Category", "Integration")]
public class RabbitMqIntegrationEventQueueTest(RabbitMqFixture fixture) : IClassFixture<RabbitMqFixture>
{
    [Fact]
    public async Task DequeueAsync_WhenEnqueued_ShouldExtractIntegrationEvent()
    {
        // Arrange
        var integrationEventQueue = CreateIntegrationEventQueue();

        var integrationEvent = new TestIntegrationEvent
        {
            TableUid = Guid.NewGuid(),
            Name = "Test Event",
            Number = 100500,
            OccuredAt = DateTime.UtcNow
        };

        // Act
        await integrationEventQueue.EnqueueAsync(integrationEvent, IntegrationEventChannel.Cash);
        var dequeuedEvent = await integrationEventQueue.DequeueAsync(IntegrationEventChannel.Cash);

        // Assert
        var typedIntegrationEvent = Assert.IsType<TestIntegrationEvent>(dequeuedEvent);
        Assert.Equal(integrationEvent, typedIntegrationEvent);
    }

    [Fact]
    public async Task DequeueAsync_WhenNotEnqueued_ShouldReturnNull()
    {
        // Arrange
        var integrationEventQueue = CreateIntegrationEventQueue();

        var integrationEvent = new TestIntegrationEvent
        {
            TableUid = Guid.NewGuid(),
            Name = "Test Event",
            Number = 100500,
            OccuredAt = DateTime.UtcNow
        };
        await integrationEventQueue.EnqueueAsync(integrationEvent, IntegrationEventChannel.Hand); // Different channel

        // Act & Assert
        var dequeuedEvent = await integrationEventQueue.DequeueAsync(IntegrationEventChannel.Cash);

        // Assert
        Assert.Null(dequeuedEvent);
    }

    private IIntegrationEventQueue CreateIntegrationEventQueue()
    {
        var options = Options.Create(fixture.CreateOptions());
        return new RabbitMqIntegrationEventQueue(
            options,
            NullLogger<RabbitMqIntegrationEventQueue>.Instance
        );
    }
}

internal record TestIntegrationEvent : IIntegrationEvent
{
    public Guid TableUid { get; init; }
    public required string Name { get; init; }
    public required int Number { get; init; }
    public required DateTime OccuredAt { get; init; }
}

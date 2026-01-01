using Application.IntegrationEvent;
using Infrastructure.IntegrationEvent;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Test.IntegrationEvent;

[Trait("Category", "Integration")]
public class RabbitMqIntegrationEventPublisherTest(
    RabbitMqFixture fixture
) : IClassFixture<RabbitMqFixture>, IAsyncLifetime
{
    private IConnection _connection = default!;
    private IChannel _channel = default!;

    private const string ExchangeType = "topic";
    private const string ExchangeName = "test.integration-event-publisher-exchange";
    private const string QueueName = "test.integration-event-publisher-queue";
    private const string RoutingKey = "test.integration-event-publisher-routing-key";

    [Fact]
    public async Task PublishAsync_WhenConnected_ShouldPublishIntegrationEvent()
    {
        // Arrange
        var publisher = CreateIntegrationEventPublisher();

        var integrationEvent = new TestPublishedIntegrationEvent
        {
            TableUid = Guid.NewGuid(),
            Name = "Test Integration Event Publisher",
            Number = 100500,
            OccuredAt = GetNow()
        };

        var consumer = new AsyncEventingBasicConsumer(_channel);
        BasicDeliverEventArgs? received = null;

        consumer.ReceivedAsync += (_, args) =>
        {
            received = args;
            return Task.CompletedTask;
        };

        await _channel.BasicConsumeAsync(
            queue: QueueName,
            autoAck: true,
            consumer: consumer
        );

        // Act
        await publisher.PublishAsync(integrationEvent, new IntegrationEventRoutingKey(RoutingKey));

        var timeout = DateTime.UtcNow.AddSeconds(5);
        while (received == null && DateTime.UtcNow < timeout)
        {
            await Task.Delay(50);
        }

        // Assert
        Assert.NotNull(received);

        var body = Encoding.UTF8.GetString(received.Body.Span);
        var publishedEvent =
            JsonSerializer.Deserialize<TestPublishedIntegrationEvent>(body, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }
        );

        Assert.NotNull(publishedEvent);
        Assert.Equal(integrationEvent, publishedEvent);
        Assert.Equal("application/json", received.BasicProperties.ContentType);
        Assert.Equal(nameof(TestPublishedIntegrationEvent), received.BasicProperties.Type);
        Assert.Equal(
            integrationEvent.OccuredAt,
            DateTimeOffset.FromUnixTimeSeconds(received.BasicProperties.Timestamp.UnixTime).UtcDateTime
        );
    }

    public async Task InitializeAsync()
    {
        var options = fixture.CreateOptions();

        var factory = new ConnectionFactory
        {
            HostName = options.Host,
            Port = options.Port,
            UserName = options.Username,
            Password = options.Password,
            VirtualHost = options.VirtualHost
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName,
            type: ExchangeType,
            durable: true,
            autoDelete: false
        );

        await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: true
        );

        await _channel.QueueBindAsync(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: RoutingKey
        );
    }

    public async Task DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }

    private IIntegrationEventPublisher CreateIntegrationEventPublisher()
    {
        var connectionOptions = Options.Create(fixture.CreateOptions());
        var publisherOptions = Options.Create(
            new RabbitMqIntegrationEventPublisherOptions
            {
                ExchangeName = ExchangeName,
                ExchangeType = ExchangeType,
                Durable = true,
                AutoDelete = false
            }
        );
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.SetMinimumLevel(LogLevel.Information).AddConsole();
        });
        var logger = loggerFactory.CreateLogger<RabbitMqIntegrationEventPublisher>();
        return new RabbitMqIntegrationEventPublisher(connectionOptions, publisherOptions, logger);
    }

    private static DateTime GetNow()
    {
        // We drop milliseconds because they are not supported in RabbitMQ
        var now = DateTime.Now;
        return new DateTime(
            now.Year,
            now.Month,
            now.Day,
            now.Hour,
            now.Minute,
            now.Second,
            now.Kind
        );
    }
}

internal record TestPublishedIntegrationEvent : IIntegrationEvent
{
    public Guid TableUid { get; init; }
    public required string Name { get; init; }
    public required int Number { get; init; }
    public required DateTime OccuredAt { get; init; }
}

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PublishAsync_WhenConnected_ShouldPublishIntegrationEvent(bool withCorrelationId)
    {
        // Arrange
        var publisher = CreateIntegrationEventPublisher();

        var integrationEvent = new TestPublishedIntegrationEvent
        {
            Uid = Guid.NewGuid(),
            CorrelationUid = withCorrelationId ? Guid.NewGuid() : null,
            OccurredAt = GetNow(),
            TableUid = Guid.NewGuid(),
            Name = "Test Published Integration Event",
            Number = 100500,
            Participants = [
                new()
                {
                    Nickname = "Nickname",
                    Seat = 1,
                    Stack = 1000
                }
            ]
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
            integrationEvent.OccurredAt,
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
    public required Guid Uid { get; init; }
    public Guid? CorrelationUid { get; init; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required string Name { get; init; }
    public required int Number { get; init; }
    public required List<IntegrationEventParticipant> Participants { get; init; }

    public virtual bool Equals(TestPublishedIntegrationEvent? other)
    {
        if (other is null)
        {
            return false;
        }

        return Uid.Equals(other.Uid)
               && CorrelationUid.Equals(other.CorrelationUid)
               && OccurredAt.Equals(other.OccurredAt)
               && TableUid.Equals(other.TableUid)
               && Name.Equals(other.Name)
               && Number.Equals(other.Number)
               && Participants.SequenceEqual(other.Participants);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();

        hash.Add(Uid);
        hash.Add(CorrelationUid);
        hash.Add(OccurredAt);

        hash.Add(TableUid);

        hash.Add(Name);
        hash.Add(Number);

        foreach (var participant in Participants)
        {
            hash.Add(participant);
        }

        return hash.ToHashCode();
    }
}

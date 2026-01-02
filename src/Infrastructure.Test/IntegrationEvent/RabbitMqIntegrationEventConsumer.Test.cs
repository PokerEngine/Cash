using Application.IntegrationEvent;
using Infrastructure.IntegrationEvent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Test.IntegrationEvent;

[Trait("Category", "Integration")]
public class RabbitMqIntegrationEventConsumerTest(
    RabbitMqFixture fixture
) : IClassFixture<RabbitMqFixture>, IAsyncLifetime
{
    private IConnection _connection = default!;
    private IChannel _channel = default!;

    private readonly string ExchangeName = $"test.integration-event-consumer-exchange.{Guid.NewGuid()}";
    private readonly string QueueName = $"test.integration-event-consumer-queue.{Guid.NewGuid()}";
    private const string RoutingKey = "test.integration-event-consumer-routing-key";

    [Fact]
    public async Task ExecuteAsync_WhenDispatchedSuccessfully_ShouldDequeue()
    {
        // Arrange
        var dispatcher = new TestIntegrationEventDispatcher();
        var consumer = CreateIntegrationEventConsumer(dispatcher);

        var integrationEvent = new TestConsumedIntegrationEvent
        {
            TableUid = Guid.NewGuid(),
            Name = "Test Integration Event Consumer",
            Number = 500100,
            OccurredAt = GetNow()
        };

        var body = JsonSerializer.SerializeToUtf8Bytes(
            integrationEvent,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        var timestamp = new DateTimeOffset(DateTime.SpecifyKind(integrationEvent.OccurredAt, DateTimeKind.Utc));
        var props = new BasicProperties
        {
            ContentType = "application/json",
            Type = nameof(TestConsumedIntegrationEvent),
            Timestamp = new AmqpTimestamp(timestamp.ToUnixTimeSeconds())
        };

        // Act
        await consumer.StartAsync(CancellationToken.None);
        await _channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: RoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body
        );
        var timeout = DateTime.UtcNow.AddSeconds(5);
        while (dispatcher.Dispatched.Count == 0 && DateTime.UtcNow < timeout)
        {
            await Task.Delay(50);
        }

        // Assert
        Assert.Single(dispatcher.Dispatched);
        var consumedEvent = Assert.IsType<TestConsumedIntegrationEvent>(dispatcher.Dispatched[0]);
        Assert.Equal(integrationEvent, consumedEvent);

        await consumer.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotResolvedType_ShouldDequeue()
    {
        // Arrange
        var dispatcher = new TestFailingIntegrationEventDispatcher();
        var consumer = CreateIntegrationEventConsumer(dispatcher);

        var body = Encoding.UTF8.GetBytes("{}");
        var props = new BasicProperties
        {
            ContentType = "application/json",
            Type = "TestUnknownIntegrationEvent",
            Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        };

        // Act
        await consumer.StartAsync(CancellationToken.None);
        await _channel.BasicPublishAsync(
            exchange: ExchangeName,
            routingKey: RoutingKey,
            mandatory: false,
            basicProperties: props,
            body: body
        );
        await Task.Delay(500);

        // Assert
        var result = await _channel.BasicGetAsync(QueueName, autoAck: true);
        Assert.Null(result); // Message should NOT be requeued → queue remains empty

        await consumer.StopAsync(CancellationToken.None);
    }

    public async Task InitializeAsync()
    {
        var consumerOptions = CreateOptions();
        var connectionOptions = fixture.CreateOptions();
        var factory = new ConnectionFactory
        {
            HostName = connectionOptions.Host,
            Port = connectionOptions.Port,
            UserName = connectionOptions.Username,
            Password = connectionOptions.Password,
            VirtualHost = connectionOptions.VirtualHost
        };

        _connection = await factory.CreateConnectionAsync();
        _channel = await _connection.CreateChannelAsync();

        foreach (var binding in consumerOptions.Bindings)
        {
            await _channel.ExchangeDeclareAsync(
                exchange: binding.ExchangeName,
                type: binding.ExchangeType,
                durable: consumerOptions.Durable,
                autoDelete: consumerOptions.AutoDelete
            );
        }

        await _channel.QueueDeclareAsync(
            queue: consumerOptions.QueueName,
            durable: consumerOptions.Durable,
            exclusive: consumerOptions.Exclusive,
            autoDelete: consumerOptions.AutoDelete
        );

        foreach (var binding in consumerOptions.Bindings)
        {
            await _channel.QueueBindAsync(
                queue: consumerOptions.QueueName,
                exchange: binding.ExchangeName,
                routingKey: binding.RoutingKey
            );
        }
    }

    public async Task DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }

    private RabbitMqIntegrationEventConsumer CreateIntegrationEventConsumer(IIntegrationEventDispatcher dispatcher)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IIntegrationEventDispatcher>(dispatcher);
        services.AddLogging();

        var provider = services.BuildServiceProvider();

        var consumerOptions = Options.Create(CreateOptions());
        var connectionOptions = Options.Create(fixture.CreateOptions());

        return new RabbitMqIntegrationEventConsumer(
            provider.GetRequiredService<IServiceScopeFactory>(),
            consumerOptions,
            connectionOptions,
            provider.GetRequiredService<ILogger<RabbitMqIntegrationEventConsumer>>()
        );
    }

    private RabbitMqIntegrationEventConsumerOptions CreateOptions()
    {
        return new RabbitMqIntegrationEventConsumerOptions
        {
            QueueName = QueueName,
            Durable = false,
            Exclusive = false,
            AutoDelete = true,
            PrefetchCount = 1,
            Bindings =
            [
                new() { ExchangeName = ExchangeName, ExchangeType = "topic", RoutingKey = RoutingKey }
            ]
        };
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

internal record TestConsumedIntegrationEvent : IIntegrationEvent
{
    public Guid TableUid { get; init; }
    public required string Name { get; init; }
    public required int Number { get; init; }
    public required DateTime OccurredAt { get; init; }
}

internal class TestIntegrationEventDispatcher : IIntegrationEventDispatcher
{
    public readonly List<IIntegrationEvent> Dispatched = new();

    public Task DispatchAsync(IIntegrationEvent integrationEvent)
    {
        Dispatched.Add(integrationEvent);
        return Task.CompletedTask;
    }
}

internal class TestFailingIntegrationEventDispatcher : IIntegrationEventDispatcher
{
    public Task DispatchAsync(IIntegrationEvent integrationEvent)
    {
        throw new InvalidOperationException("Boom");
    }
}

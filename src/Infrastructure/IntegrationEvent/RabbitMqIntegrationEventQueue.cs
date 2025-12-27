using Application.IntegrationEvent;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Infrastructure.IntegrationEvent;

public class RabbitMqIntegrationEventQueue : IIntegrationEventQueue, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqIntegrationEventQueue> _logger;

    public RabbitMqIntegrationEventQueue(
        IOptions<RabbitMqIntegrationEventQueueOptions> options,
        ILogger<RabbitMqIntegrationEventQueue> logger
    )
    {
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = options.Value.Host,
            Port = options.Value.Port,
            UserName = options.Value.Username,
            Password = options.Value.Password,
            VirtualHost = options.Value.VirtualHost
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();

        DeclareQueuesAsync().GetAwaiter().GetResult();
    }

    public async Task EnqueueAsync(
        IIntegrationEvent integrationEvent,
        IntegrationEventChannel channel,
        CancellationToken cancellationToken = default
    )
    {
        await using var rabbitChannel = await _connection.CreateChannelAsync(null, cancellationToken);

        var queueName = channel.ToString();

        var envelope = new IntegrationEventEnvelope
        {
            Type = integrationEvent.GetType().AssemblyQualifiedName!,
            Data = JsonSerializer.SerializeToElement(
                integrationEvent,
                integrationEvent.GetType()
            ),
            OccurredAt = integrationEvent.OccuredAt
        };

        var body = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(envelope)
        );

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        await rabbitChannel.BasicPublishAsync(
            exchange: "",
            routingKey: queueName,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken
        );

        _logger.LogInformation(
            "Published {EventType} to queue {Queue}",
            integrationEvent.GetType().Name,
            queueName
        );
    }

    public async Task<IIntegrationEvent> DequeueAsync(
        IntegrationEventChannel channel,
        CancellationToken cancellationToken = default
    )
    {
        await using var rabbitChannel = await _connection.CreateChannelAsync(
            null,
            cancellationToken
        );

        var queueName = channel.ToString();

        var result = await rabbitChannel.BasicGetAsync(
            queue: queueName,
            autoAck: false,
            cancellationToken: cancellationToken
        );

        if (result is null)
        {
            throw new InvalidOperationException($"Queue {queueName} is empty");
        }

        try
        {
            var json = Encoding.UTF8.GetString(result.Body.ToArray());

            var envelope = JsonSerializer.Deserialize<IntegrationEventEnvelope>(json);
            if (envelope is null)
            {
                throw new InvalidOperationException("Failed to deserialize envelope");
            }

            var eventType = Type.GetType(envelope.Type, throwOnError: true)!;
            var integrationEvent = (IIntegrationEvent?)envelope.Data.Deserialize(eventType);

            if (integrationEvent is null)
            {
                throw new InvalidOperationException($"Failed to deserialize {eventType}");
            }

            // Ack only AFTER successful processing
            await rabbitChannel.BasicAckAsync(
                deliveryTag: result.DeliveryTag,
                multiple: false,
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "Dequeued {EventType} from queue {Queue}",
                eventType.Name,
                queueName
            );

            return integrationEvent;
        }
        catch
        {
            // Requeue on failure (at-least-once)
            await rabbitChannel.BasicNackAsync(
                deliveryTag: result.DeliveryTag,
                multiple: false,
                requeue: true,
                cancellationToken: cancellationToken
            );

            throw;
        }
    }


    private async Task DeclareQueuesAsync()
    {
        await using var channel = await _connection.CreateChannelAsync();

        foreach (var queueName in Enum.GetNames<IntegrationEventChannel>())
        {
            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );

            _logger.LogInformation("Declared queue {Queue}", queueName);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _connection.CloseAsync();
        _connection.Dispose();
    }
}

public class RabbitMqIntegrationEventQueueOptions
{
    public const string SectionName = "RabbitMQ";

    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string VirtualHost { get; init; }
}

internal record IntegrationEventEnvelope
{
    public required string Type { get; init; }
    public required JsonElement Data { get; init; }
    public required DateTime OccurredAt { get; init; }
}

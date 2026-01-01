using Application.IntegrationEvent;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Infrastructure.IntegrationEvent;

public class RabbitMqIntegrationEventPublisher : IIntegrationEventPublisher, IAsyncDisposable
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private readonly RabbitMqIntegrationEventPublisherOptions _options;
    private readonly IConnection _connection;
    private readonly IChannel _channel;

    public RabbitMqIntegrationEventPublisher(
        IOptions<RabbitMqConnectionOptions> connectionOptions,
        IOptions<RabbitMqIntegrationEventPublisherOptions> options
    )
    {
        _options = options.Value;

        var factory = new ConnectionFactory
        {
            HostName = connectionOptions.Value.Host,
            Port = connectionOptions.Value.Port,
            UserName = connectionOptions.Value.Username,
            Password = connectionOptions.Value.Password,
            VirtualHost = connectionOptions.Value.VirtualHost
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.ExchangeDeclareAsync(
            exchange: _options.ExchangeName,
            type: _options.ExchangeType,
            durable: _options.Durable,
            autoDelete: _options.AutoDelete
        ).GetAwaiter().GetResult();
    }

    public async Task PublishAsync(
        IIntegrationEvent integrationEvent,
        IntegrationEventRoutingKey routingKey,
        CancellationToken cancellationToken = default
    )
    {
        var body = Encoding.UTF8.GetBytes(Serialize(integrationEvent));
        var type = integrationEvent.GetType().AssemblyQualifiedName!;
        var timestamp = new DateTimeOffset(DateTime.SpecifyKind(integrationEvent.OccuredAt, DateTimeKind.Utc));

        var properties = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent,
            Type = type,
            Timestamp = new AmqpTimestamp(timestamp.ToUnixTimeSeconds())
        };

        await _channel.BasicPublishAsync(
            exchange: _options.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body,
            cancellationToken: cancellationToken
        );
    }

    private string Serialize(IIntegrationEvent integrationEvent)
    {
        return JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), JsonSerializerOptions);
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel.IsOpen)
        {
            await _channel.CloseAsync();
        }

        if (_connection.IsOpen)
        {
            await _connection.CloseAsync();
        }
    }
}

public class RabbitMqIntegrationEventPublisherOptions
{
    public const string SectionName = "RabbitMqIntegrationEventPublisher";

    public required string ExchangeName { get; init; }
    public required string ExchangeType { get; init; }
    public bool Durable { get; init; } = true;
    public bool AutoDelete { get; init; } = false;
}

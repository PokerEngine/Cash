using Application.IntegrationEvent;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Infrastructure.IntegrationEvent;

public class RabbitMqIntegrationEventConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqIntegrationEventConsumerOptions> options,
    IOptions<RabbitMqConnectionOptions> connectionOptions,
    ILogger<RabbitMqIntegrationEventConsumer> logger
) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("IntegrationEventConsumer started");

        var factory = new ConnectionFactory
        {
            HostName = connectionOptions.Value.Host,
            Port = connectionOptions.Value.Port,
            UserName = connectionOptions.Value.Username,
            Password = connectionOptions.Value.Password,
            VirtualHost = connectionOptions.Value.VirtualHost
        };
        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(null, cancellationToken);
        await DeclareTopologyAsync(channel);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += OnMessageAsync;

        await channel.BasicQosAsync(
            prefetchSize: options.Value.PrefetchSize,
            prefetchCount: options.Value.PrefetchCount,
            global: false,
            cancellationToken: cancellationToken
        );

        await channel.BasicConsumeAsync(
            queue: options.Value.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken
        );
        await Task.Delay(Timeout.Infinite, cancellationToken);

        logger.LogInformation("IntegrationEventConsumer stopped");

        async Task OnMessageAsync(object sender, BasicDeliverEventArgs args)
        {
            try
            {
                logger.LogInformation("Consuming {Args} from {Sender}", args, sender);

                var integrationEvent = Deserialize(args);
                using var scope = scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IIntegrationEventDispatcher>();
                await dispatcher.DispatchAsync(integrationEvent);

                await channel.BasicAckAsync(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    cancellationToken: cancellationToken
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process {Type}", args.BasicProperties.Type);

                await channel.BasicNackAsync(
                    deliveryTag: args.DeliveryTag,
                    multiple: false,
                    requeue: true,
                    cancellationToken: cancellationToken
                );
            }
        }
    }

    private IIntegrationEvent Deserialize(BasicDeliverEventArgs args)
    {
        var type = RabbitMqIntegrationEventTypeResolver.GetType(args.BasicProperties.Type!);
        var json = Encoding.UTF8.GetString(args.Body.Span);
        return (IIntegrationEvent)JsonSerializer.Deserialize(json, type, JsonSerializerOptions)!;
    }

    private async Task DeclareTopologyAsync(IChannel channel)
    {
        await channel.QueueDeclareAsync(
            queue: options.Value.QueueName,
            durable: options.Value.Durable,
            exclusive: options.Value.Exclusive,
            autoDelete: options.Value.AutoDelete
        );

        foreach (var binding in options.Value.Bindings)
        {
            await channel.QueueBindAsync(
                queue: options.Value.QueueName,
                exchange: binding.ExchangeName,
                routingKey: binding.RoutingKey
            );
        }
    }
}

public class RabbitMqIntegrationEventConsumerOptions
{
    public const string SectionName = "RabbitMqIntegrationEventConsumer";

    public required string QueueName { get; init; }
    public bool Durable { get; init; } = true;
    public bool Exclusive { get; init; } = false;
    public bool AutoDelete { get; init; } = false;
    public uint PrefetchSize { get; init; } = 0;
    public ushort PrefetchCount { get; init; } = 10;
    public RabbitMqIntegrationEventConsumerBindingOptions[] Bindings { get; init; } = [];
}

public class RabbitMqIntegrationEventConsumerBindingOptions
{
    public required string ExchangeName { get; init; }
    public required string RoutingKey { get; init; }
}

using Application.IntegrationEvent;
using Infrastructure.Client.RabbitMq;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace Infrastructure.IntegrationEvent;

public class RabbitMqIntegrationEventConsumer(
    IServiceScopeFactory scopeFactory,
    IOptions<RabbitMqIntegrationEventConsumerOptions> options,
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

        using var scope = scopeFactory.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<RabbitMqClient>();
        await using var connection = await client.Factory.CreateConnectionAsync(cancellationToken);
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
                logger.LogInformation("Consuming {Type}", args.BasicProperties.Type);

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
        foreach (var binding in options.Value.Bindings)
        {
            logger.LogInformation(
                "Declaring exchange {ExchangeName} ({ExchangeType})",
                binding.ExchangeName,
                binding.ExchangeType
            );
            await channel.ExchangeDeclareAsync(
                exchange: binding.ExchangeName,
                type: binding.ExchangeType,
                durable: options.Value.Durable,
                autoDelete: options.Value.AutoDelete
            );
        }

        logger.LogInformation("Declaring queue {QueueName}", options.Value.QueueName);
        await channel.QueueDeclareAsync(
            queue: options.Value.QueueName,
            durable: options.Value.Durable,
            exclusive: options.Value.Exclusive,
            autoDelete: options.Value.AutoDelete
        );

        foreach (var binding in options.Value.Bindings)
        {
            logger.LogInformation(
                "Binding queue {QueueName} with exchange {ExchangeName} / {RoutingKey}",
                options.Value.QueueName,
                binding.ExchangeName,
                binding.RoutingKey
            );
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
    public required string ExchangeType { get; init; }
    public required string RoutingKey { get; init; }
}

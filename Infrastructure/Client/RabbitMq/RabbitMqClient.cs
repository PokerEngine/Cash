using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace Infrastructure.Client.RabbitMq;

public class RabbitMqClient
{
    public ConnectionFactory Factory;

    public RabbitMqClient(IOptions<RabbitMqClientOptions> options)
    {
        Factory = new ConnectionFactory
        {
            HostName = options.Value.Host,
            Port = options.Value.Port,
            UserName = options.Value.Username,
            Password = options.Value.Password,
            VirtualHost = options.Value.VirtualHost
        };
    }
}

public class RabbitMqClientOptions
{
    public const string SectionName = "RabbitMq";

    public required string Host { get; init; }
    public required int Port { get; init; }
    public required string Username { get; init; }
    public required string Password { get; init; }
    public required string VirtualHost { get; init; }
}

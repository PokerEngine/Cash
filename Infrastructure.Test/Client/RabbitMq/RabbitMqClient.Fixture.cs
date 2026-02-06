using Infrastructure.Client.RabbitMq;
using Microsoft.Extensions.Options;
using Testcontainers.RabbitMq;

namespace Infrastructure.Test.Client.RabbitMq;

public sealed class RabbitMqClientFixture : IAsyncLifetime
{
    private const string Username = "guest";
    private const string Password = "guest";
    private const int Port = 5672;

    private RabbitMqContainer Container { get; } =
        new RabbitMqBuilder()
            .WithImage("rabbitmq:4-management")
            .WithUsername(Username)
            .WithPassword(Password)
            .Build();

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }

    public RabbitMqClient CreateClient()
    {
        var options = CreateOptions();
        return new RabbitMqClient(options);
    }

    private IOptions<RabbitMqClientOptions> CreateOptions()
    {
        var options = new RabbitMqClientOptions
        {
            Host = Container.Hostname,
            Port = Container.GetMappedPublicPort(Port),
            Username = Username,
            Password = Password,
            VirtualHost = "/"
        };
        return Options.Create(options);
    }
}

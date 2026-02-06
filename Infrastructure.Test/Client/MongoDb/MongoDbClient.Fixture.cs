using Infrastructure.Client.MongoDb;
using Microsoft.Extensions.Options;
using Testcontainers.MongoDb;

namespace Infrastructure.Test.Client.MongoDb;

public sealed class MongoDbClientFixture : IAsyncLifetime
{
    private const string Username = "guest";
    private const string Password = "guest";
    private const int Port = 27017;

    private MongoDbContainer Container { get; } =
        new MongoDbBuilder()
            .WithImage("mongo:8")
            .WithUsername(Username)
            .WithPassword(Password)
            .WithCleanUp(true)
            .Build();

    public async Task InitializeAsync()
    {
        await Container.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await Container.DisposeAsync();
    }

    public MongoDbClient CreateClient()
    {
        var options = CreateOptions();
        return new MongoDbClient(options);
    }

    private IOptions<MongoDbClientOptions> CreateOptions()
    {
        var options = new MongoDbClientOptions
        {
            Host = Container.Hostname,
            Port = Container.GetMappedPublicPort(Port),
            Username = Username,
            Password = Password
        };
        return Options.Create(options);
    }
}

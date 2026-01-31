using Infrastructure.Repository;
using Testcontainers.MongoDb;

namespace Infrastructure.Test.Repository;

public sealed class MongoDbFixture : IAsyncLifetime
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

    public MongoDbRepositoryOptions CreateOptions()
    {
        var databaseName = $"test-db-{Guid.NewGuid()}";
        return new MongoDbRepositoryOptions
        {
            Host = Container.Hostname,
            Port = Container.GetMappedPublicPort(Port),
            Username = Username,
            Password = Password,
            Database = databaseName
        };
    }
}

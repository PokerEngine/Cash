using Application.Command;
using Application.Connection;
using Application.IntegrationEvent;
using Application.Query;
using Application.Repository;
using Application.Service.Hand;
using Infrastructure.Command;
using Infrastructure.Connection;
using Infrastructure.Controller;
using Infrastructure.IntegrationEvent;
using Infrastructure.Query;
using Infrastructure.Repository;
using Infrastructure.Service.Hand;

namespace Infrastructure;

public static class Bootstrapper
{
    public static WebApplicationBuilder PrepareApplicationBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddEnvironmentVariables();
        builder.Services.AddOpenApi();

        // Register dependencies
        builder.Services.AddSingleton<IRepository, InMemoryRepository>();

        builder.Services.Configure<RemoteHandServiceOptions>(
            builder.Configuration.GetSection(RemoteHandServiceOptions.SectionName)
        );
        builder.Services.AddHttpClient<IHandService>();
        builder.Services.AddSingleton<IHandService, RemoteHandService>();

        builder.Services.AddSingleton<IConnectionRegistry, InMemoryConnectionRegistry>();

        // Register commands
        RegisterCommandHandler<CreateTableCommand, CreateTableHandler, CreateTableResponse>(builder.Services);
        RegisterCommandHandler<SitDownPlayerCommand, SitDownPlayerHandler, SitDownPlayerResponse>(builder.Services);
        RegisterCommandHandler<StandUpPlayerCommand, StandUpPlayerHandler, StandUpPlayerResponse>(builder.Services);
        builder.Services.AddScoped<CommandDispatcher>();

        // Register queries
        RegisterQueryHandler<GetTableByUidQuery, GetTableByUidHandler, GetTableByUidResponse>(builder.Services);
        builder.Services.AddScoped<QueryDispatcher>();

        // Register integration events
        RegisterIntegrationEventHandler<HandIsCreatedIntegrationEvent, HandIsCreatedHandler>(builder.Services);
        RegisterIntegrationEventHandler<HandIsStartedIntegrationEvent, HandIsStartedHandler>(builder.Services);
        RegisterIntegrationEventHandler<HandIsFinishedIntegrationEvent, HandIsFinishedHandler>(builder.Services);
        RegisterIntegrationEventHandler<DecisionIsCommittedIntegrationEvent, DecisionIsCommittedHandler>(builder.Services);
        RegisterIntegrationEventHandler<PlayerSatDownIntegrationEvent, PlayerSatDownHandler>(builder.Services);
        RegisterIntegrationEventHandler<PlayerSatOutIntegrationEvent, PlayerSatOutHandler>(builder.Services);
        RegisterIntegrationEventHandler<PlayerSatInIntegrationEvent, PlayerSatInHandler>(builder.Services);
        RegisterIntegrationEventHandler<PlayerStoodUpIntegrationEvent, PlayerStoodUpHandler>(builder.Services);
        builder.Services.AddScoped<IntegrationEventDispatcher>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        // Add websocket controllers
        builder.Services.AddScoped<TableWsController>();

        return builder;
    }

    private static void RegisterCommandHandler<TCommand, THandler, TResponse>(IServiceCollection services)
        where TCommand : ICommandRequest
        where TResponse : ICommandResponse
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        services.AddScoped<THandler>();
        services.AddScoped<ICommandHandler<TCommand, TResponse>>(provider => provider.GetRequiredService<THandler>());
    }

    private static void RegisterQueryHandler<TQuery, THandler, TResponse>(IServiceCollection services)
        where TQuery : IQueryRequest
        where TResponse : IQueryResponse
        where THandler : class, IQueryHandler<TQuery, TResponse>
    {
        services.AddScoped<THandler>();
        services.AddScoped<IQueryHandler<TQuery, TResponse>>(provider => provider.GetRequiredService<THandler>());
    }

    private static void RegisterIntegrationEventHandler<TIntegrationEvent, THandler>(IServiceCollection services)
        where TIntegrationEvent : IIntegrationEvent
        where THandler : class, IIntegrationEventHandler<TIntegrationEvent>
    {
        services.AddScoped<THandler>();
        services.AddScoped<IIntegrationEventHandler<TIntegrationEvent>>(provider => provider.GetRequiredService<THandler>());
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var app = CreateWebApplication(args);
        app.Run();
    }

    // Public method for creating the WebApplication - can be called by tests
    // This allows WebApplicationFactory to work properly with the minimal hosting model
    private static WebApplication CreateWebApplication(string[] args)
    {
        var builder = Bootstrapper.PrepareApplicationBuilder(args);
        return ConfigureApplication(builder);
    }

    // Configure the application pipeline
    private static WebApplication ConfigureApplication(WebApplicationBuilder builder)
    {
        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        app.UseWebSockets();

        app.MapOpenApi();
        app.MapControllers();

        // Map websocket endpoints
        app.Map("/ws/table/{uid:guid}/events/{nickname}", async (
            HttpContext context,
            Guid uid,
            string nickname,
            TableWsController controller,
            CancellationToken cancellationToken) =>
        {
            await controller.HandleAsync(
                context: context,
                uid: uid,
                nickname: nickname,
                cancellationToken: cancellationToken
            );
        });

        return app;
    }
}

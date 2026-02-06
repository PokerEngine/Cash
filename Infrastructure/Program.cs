using Application.Command;
using Application.Connection;
using Application.Event;
using Application.IntegrationEvent;
using Application.Query;
using Application.Repository;
using Application.Service.Hand;
using Domain.Event;
using Infrastructure.Client.MongoDb;
using Infrastructure.Client.RabbitMq;
using Infrastructure.Command;
using Infrastructure.Connection;
using Infrastructure.Controller;
using Infrastructure.Event;
using Infrastructure.IntegrationEvent;
using Infrastructure.Query;
using Infrastructure.Repository;
using Infrastructure.Service.Hand;
using Microsoft.Extensions.Options;

namespace Infrastructure;

public static class Bootstrapper
{
    public static WebApplicationBuilder PrepareApplicationBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration.AddEnvironmentVariables();
        builder.Services.AddOpenApi();

        // Register clients
        builder.Services.Configure<MongoDbClientOptions>(
            builder.Configuration.GetSection(MongoDbClientOptions.SectionName)
        );
        builder.Services.AddSingleton<MongoDbClient>();
        builder.Services.Configure<RabbitMqClientOptions>(
            builder.Configuration.GetSection(RabbitMqClientOptions.SectionName)
        );
        builder.Services.AddSingleton<RabbitMqClient>();

        // Register repository
        builder.Services.Configure<MongoDbRepositoryOptions>(
            builder.Configuration.GetSection(MongoDbRepositoryOptions.SectionName)
        );
        builder.Services.AddSingleton<IRepository, MongoDbRepository>();

        // Register connection registry
        builder.Services.AddSingleton<IConnectionRegistry, InMemoryConnectionRegistry>();

        // Register hand service
        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddSingleton<IHandService, InMemoryHandService>();
        }
        else
        {
            builder.Services.Configure<RemoteHandServiceOptions>(
                builder.Configuration.GetSection(RemoteHandServiceOptions.SectionName)
            );
            builder.Services.AddHttpClient<IHandService>();
            builder.Services.AddSingleton<IHandService, RemoteHandService>();
        }

        // Register commands
        RegisterCommandHandler<CreateTableCommand, CreateTableHandler, CreateTableResponse>(builder.Services);
        RegisterCommandHandler<SitPlayerDownCommand, SitPlayerDownHandler, SitPlayerDownResponse>(builder.Services);
        RegisterCommandHandler<StandPlayerUpCommand, StandPlayerUpHandler, StandPlayerUpResponse>(builder.Services);
        RegisterCommandHandler<SubmitPlayerActionCommand, SubmitPlayerActionHandler, SubmitPlayerActionResponse>(builder.Services);
        builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();

        // Register queries
        RegisterQueryHandler<GetTableByUidQuery, GetTableByUidHandler, GetTableByUidResponse>(builder.Services);
        builder.Services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        // Register domain events
        RegisterEventHandler<TableCreatedEvent, TableCreatedEventHandler>(builder.Services);
        RegisterEventHandler<PlayerSatDownEvent, PlayerSatDownEventHandler>(builder.Services);
        RegisterEventHandler<PlayerStoodUpEvent, PlayerStoodUpEventHandler>(builder.Services);
        builder.Services.AddScoped<IEventDispatcher, EventDispatcher>();

        // Register integration events
        RegisterIntegrationEventHandler<PlayerSatDownIntegrationEvent, PlayerSatDownHandler>(builder.Services);
        RegisterIntegrationEventHandler<PlayerSatOutIntegrationEvent, PlayerSatOutHandler>(builder.Services);
        RegisterIntegrationEventHandler<PlayerSatInIntegrationEvent, PlayerSatInHandler>(builder.Services);
        RegisterIntegrationEventHandler<PlayerStoodUpIntegrationEvent, PlayerStoodUpHandler>(builder.Services);
        RegisterIntegrationEventHandler<HandStartedIntegrationEvent, HandStartedHandler>(builder.Services);
        RegisterIntegrationEventHandler<HandFinishedIntegrationEvent, HandFinishedHandler>(builder.Services);
        RegisterIntegrationEventHandler<BlindPostedIntegrationEvent, BlindPostedHandler>(builder.Services);
        RegisterIntegrationEventHandler<HoleCardsDealtIntegrationEvent, HoleCardsDealtHandler>(builder.Services);
        RegisterIntegrationEventHandler<HoleCardsShownIntegrationEvent, HoleCardsShownHandler>(builder.Services);
        RegisterIntegrationEventHandler<HoleCardsMuckedIntegrationEvent, HoleCardsMuckedHandler>(builder.Services);
        RegisterIntegrationEventHandler<BoardCardsDealtIntegrationEvent, BoardCardsDealtHandler>(builder.Services);
        RegisterIntegrationEventHandler<PlayerActionRequestedIntegrationEvent, PlayerActionRequestedHandler>(builder.Services);
        RegisterIntegrationEventHandler<PlayerActedIntegrationEvent, PlayerActedHandler>(builder.Services);
        RegisterIntegrationEventHandler<BetRefundedIntegrationEvent, BetRefundedHandler>(builder.Services);
        RegisterIntegrationEventHandler<SidePotAwardedIntegrationEvent, SidePotAwardedHandler>(builder.Services);
        builder.Services.AddScoped<IIntegrationEventDispatcher, IntegrationEventDispatcher>();

        builder.Services.Configure<RabbitMqIntegrationEventPublisherOptions>(
            builder.Configuration.GetSection(RabbitMqIntegrationEventPublisherOptions.SectionName)
        );
        builder.Services.Configure<RabbitMqIntegrationEventConsumerOptions>(
            builder.Configuration.GetSection(RabbitMqIntegrationEventConsumerOptions.SectionName)
        );
        builder.Services.AddScoped<IIntegrationEventPublisher, RabbitMqIntegrationEventPublisher>();
        builder.Services.AddHostedService(provider =>
            new RabbitMqIntegrationEventConsumer(
                scopeFactory: provider.GetRequiredService<IServiceScopeFactory>(),
                options: provider.GetRequiredService<IOptions<RabbitMqIntegrationEventConsumerOptions>>(),
                logger: provider.GetRequiredService<ILogger<RabbitMqIntegrationEventConsumer>>()
            )
        );

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        // Register websocket controller
        builder.Services.AddScoped<TableWsController>();

        return builder;
    }

    private static void RegisterCommandHandler<TCommand, THandler, TResponse>(IServiceCollection services)
        where TCommand : ICommand
        where TResponse : ICommandResponse
        where THandler : class, ICommandHandler<TCommand, TResponse>
    {
        services.AddScoped<THandler>();
        services.AddScoped<ICommandHandler<TCommand, TResponse>>(provider => provider.GetRequiredService<THandler>());
    }

    private static void RegisterQueryHandler<TQuery, THandler, TResponse>(IServiceCollection services)
        where TQuery : IQuery
        where TResponse : IQueryResponse
        where THandler : class, IQueryHandler<TQuery, TResponse>
    {
        services.AddScoped<THandler>();
        services.AddScoped<IQueryHandler<TQuery, TResponse>>(provider => provider.GetRequiredService<THandler>());
    }

    private static void RegisterEventHandler<TEvent, THandler>(IServiceCollection services)
        where TEvent : IEvent
        where THandler : class, IEventHandler<TEvent>
    {
        services.AddScoped<THandler>();
        services.AddScoped<IEventHandler<TEvent>>(provider => provider.GetRequiredService<THandler>());
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

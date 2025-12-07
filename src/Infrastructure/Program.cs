using Application.Command;
using Application.Query;
using Application.Repository;
using Application.Service.Hand;
using Infrastructure.Command;
using Infrastructure.Controller;
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

        void RegisterCommandHandler<TCommandRequest, TCommandHandler, TCommandResponse>(IServiceCollection services)
            where TCommandRequest : ICommandRequest
            where TCommandResponse : ICommandResponse
            where TCommandHandler : class, ICommandHandler<TCommandRequest, TCommandResponse>
        {
            services.AddScoped<TCommandHandler>();
            services.AddScoped<ICommandHandler<TCommandRequest, TCommandResponse>>(provider => provider.GetRequiredService<TCommandHandler>());
        }

        void RegisterQueryHandler<TQueryRequest, TQueryHandler, TQueryResponse>(IServiceCollection services)
            where TQueryRequest : IQueryRequest
            where TQueryResponse : IQueryResponse
            where TQueryHandler : class, IQueryHandler<TQueryRequest, TQueryResponse>
        {
            services.AddScoped<TQueryHandler>();
            services.AddScoped<IQueryHandler<TQueryRequest, TQueryResponse>>(provider => provider.GetRequiredService<TQueryHandler>());
        }

        // Register commands
        RegisterCommandHandler<CreateTableCommand, CreateTableHandler, CreateTableResult>(builder.Services);
        RegisterCommandHandler<SitDownAtTableCommand, SitDownAtTableHandler, SitDownAtTableResult>(builder.Services);
        RegisterCommandHandler<StandUpFromTableCommand, StandUpFromTableHandler, StandUpFromTableResult>(builder.Services);
        builder.Services.AddScoped<CommandDispatcher>();

        // Register queries
        RegisterQueryHandler<GetTableByUidQuery, GetTableByUidHandler, GetTableByUidResponse>(builder.Services);
        builder.Services.AddScoped<QueryDispatcher>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        // Add websocket controllers
        builder.Services.AddScoped<TableWsController>();

        return builder;
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
        app.Map("/ws/table/{uid:guid}", async (
            HttpContext context,
            Guid uid,
            TableWsController controller,
            CancellationToken cancellationToken) =>
        {
            await controller.HandleAsync(
                context: context,
                uid: uid,
                cancellationToken: cancellationToken
            );
        });

        return app;
    }
}

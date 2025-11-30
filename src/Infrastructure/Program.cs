using Application.Command;
using Application.Query;
using Application.Repository;
using Application.Service.Hand;
using Infrastructure.Command;
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

        void RegisterCommandHandler<TCommand, THandler, TResult>(IServiceCollection services)
            where TCommand : class
            where THandler : class, ICommandHandler<TCommand, TResult>
        {
            services.AddScoped<THandler>();
            services.AddScoped<ICommandHandler<TCommand, TResult>>(provider => provider.GetRequiredService<THandler>());
        }

        void RegisterQueryHandler<TQuery, THandler, TResult>(IServiceCollection services)
            where TQuery : class
            where THandler : class, IQueryHandler<TQuery, TResult>
        {
            services.AddScoped<THandler>();
            services.AddScoped<IQueryHandler<TQuery, TResult>>(provider => provider.GetRequiredService<THandler>());
        }

        // Register commands
        RegisterCommandHandler<CreateTableCommand, CreateTableHandler, CreateTableResult>(builder.Services);
        RegisterCommandHandler<SitDownAtTableCommand, SitDownAtTableHandler, SitDownAtTableResult>(builder.Services);
        RegisterCommandHandler<StandUpFromTableCommand, StandUpFromTableHandler, StandUpFromTableResult>(builder.Services);
        builder.Services.AddScoped<ICommandDispatcher, CommandDispatcher>();

        // Register queries
        RegisterQueryHandler<GetTableByUidQuery, GetTableByUidHandler, GetTableByUidResponse>(builder.Services);
        builder.Services.AddScoped<IQueryDispatcher, QueryDispatcher>();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

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

        app.MapOpenApi();
        app.MapControllers();

        return app;
    }
}

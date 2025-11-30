using Application.Query;

namespace Infrastructure.Query;

public class QueryDispatcher(
    IServiceProvider serviceProvider,
    ILogger<QueryDispatcher> logger
) : IQueryDispatcher
{
    public async Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query)
    {
        logger.LogInformation($"Dispatching query {typeof(TQuery).Name}");

        var handlerType = typeof(IQueryHandler<TQuery, TResult>);
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            throw new InvalidOperationException($"No handler found for query {typeof(TQuery).Name}");
        }

        return await ((IQueryHandler<TQuery, TResult>)handler).HandleAsync(query);
    }
}

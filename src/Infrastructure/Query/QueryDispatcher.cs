using Application.Query;

namespace Infrastructure.Query;

public class QueryDispatcher(
    IServiceProvider serviceProvider,
    ILogger<QueryDispatcher> logger
)
{
    public async Task<TResponse> DispatchAsync<TQuery, TResponse>(TQuery query)
    where TQuery : IQueryRequest
    where TResponse : IQueryResponse
    {
        logger.LogInformation($"Dispatching query {typeof(TQuery).Name}");

        var handlerType = typeof(IQueryHandler<TQuery, TResponse>);
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            throw new InvalidOperationException($"No handler found for query {typeof(TQuery).Name}");
        }

        return await ((IQueryHandler<TQuery, TResponse>)handler).HandleAsync(query);
    }
}

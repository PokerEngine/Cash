using Application.Query;

namespace Infrastructure.Query;

public class QueryDispatcher(
    IServiceProvider serviceProvider,
    ILogger<QueryDispatcher> logger
)
{
    public async Task<TResponse> DispatchAsync<TQueryRequest, TResponse>(TQueryRequest query)
    where TQueryRequest : IQueryRequest
    where TResponse : IQueryResponse
    {
        logger.LogInformation($"Dispatching query {typeof(TQueryRequest).Name}");

        var handlerType = typeof(IQueryHandler<TQueryRequest, TResponse>);
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            throw new InvalidOperationException($"No handler found for query {typeof(TQueryRequest).Name}");
        }

        return await ((IQueryHandler<TQueryRequest, TResponse>)handler).HandleAsync(query);
    }
}

using Application.Query;

namespace Infrastructure.Query;

public class QueryDispatcher(
    IServiceProvider serviceProvider,
    ILogger<QueryDispatcher> logger
) : IQueryDispatcher
{
    public async Task<TResponse> DispatchAsync<TQuery, TResponse>(TQuery query)
        where TQuery : IQueryRequest
        where TResponse : IQueryResponse
    {
        logger.LogInformation("Dispatching {QueryName}", typeof(TQuery).Name);

        var handlerType = typeof(IQueryHandler<TQuery, TResponse>);
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            throw new InvalidOperationException("Handler is not found");
        }

        return await ((IQueryHandler<TQuery, TResponse>)handler).HandleAsync(query);
    }
}

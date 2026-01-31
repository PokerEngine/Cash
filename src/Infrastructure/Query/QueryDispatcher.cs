using Application.Exception;
using Application.Query;

namespace Infrastructure.Query;

public class QueryDispatcher(
    IServiceProvider serviceProvider,
    ILogger<QueryDispatcher> logger
) : IQueryDispatcher
{
    public async Task<TResponse> DispatchAsync<TQuery, TResponse>(TQuery query)
        where TQuery : IQuery
        where TResponse : IQueryResponse
    {
        logger.LogInformation("Dispatching {QueryName}", typeof(TQuery).Name);

        var handlerType = typeof(IQueryHandler<TQuery, TResponse>);
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            throw new InternalSystemMisconfiguredException($"Handler is not found for {query.GetType().Name}");
        }

        return await ((IQueryHandler<TQuery, TResponse>)handler).HandleAsync(query);
    }
}

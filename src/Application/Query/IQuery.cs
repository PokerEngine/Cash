namespace Application.Query;

public interface IQueryRequest;

public interface IQueryResponse;

public interface IQueryHandler<in TQueryRequest, TQueryResponse>
    where TQueryRequest : IQueryRequest
    where TQueryResponse : IQueryResponse
{
    Task<TQueryResponse> HandleAsync(TQueryRequest query);
}

public interface IQueryDispatcher
{
    Task<TResponse> DispatchAsync<TQuery, TResponse>(TQuery query)
        where TQuery : IQueryRequest
        where TResponse : IQueryResponse;
}

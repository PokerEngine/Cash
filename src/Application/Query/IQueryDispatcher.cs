namespace Application.Query;

public interface IQueryDispatcher
{
    Task<TResponse> DispatchAsync<TQuery, TResponse>(TQuery query)
        where TQuery : IQueryRequest
        where TResponse : IQueryResponse;
}

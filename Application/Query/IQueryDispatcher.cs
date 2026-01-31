namespace Application.Query;

public interface IQueryDispatcher
{
    Task<TResponse> DispatchAsync<TQuery, TResponse>(TQuery query)
        where TQuery : IQuery
        where TResponse : IQueryResponse;
}

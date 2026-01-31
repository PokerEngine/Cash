namespace Application.Query;

public interface IQueryHandler<in TQuery, TQueryResponse>
    where TQuery : IQuery
    where TQueryResponse : IQueryResponse
{
    Task<TQueryResponse> HandleAsync(TQuery query);
}

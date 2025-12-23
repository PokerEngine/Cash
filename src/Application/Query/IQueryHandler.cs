namespace Application.Query;

public interface IQueryHandler<in TQueryRequest, TQueryResponse>
    where TQueryRequest : IQueryRequest
    where TQueryResponse : IQueryResponse
{
    Task<TQueryResponse> HandleAsync(TQueryRequest query);
}

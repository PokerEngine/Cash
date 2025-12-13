namespace Application.Query;

public interface IQueryRequest;

public interface IQueryResponse;

public interface IQueryHandler<in TQueryRequest, TQueryResponse>
    where TQueryRequest : IQueryRequest
    where TQueryResponse : IQueryResponse
{
    Task<TQueryResponse> HandleAsync(TQueryRequest query);
}

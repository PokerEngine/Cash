namespace Application.Query;


public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> HandleAsync(TQuery query);
}

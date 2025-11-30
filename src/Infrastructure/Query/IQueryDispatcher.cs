namespace Infrastructure.Query;

public interface IQueryDispatcher
{
    Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query);
}

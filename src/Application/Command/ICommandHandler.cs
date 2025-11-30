namespace Application.Command;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command);
}

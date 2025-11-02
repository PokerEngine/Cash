namespace Application.Command;

public interface ICommand;
public interface IResult;

public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> HandleAsync(TCommand command);
}

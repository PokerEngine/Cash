namespace Application.Command;

public interface ICommandHandler<in TCommand, TCommandResponse>
    where TCommand : ICommand
    where TCommandResponse : ICommandResponse
{
    Task<TCommandResponse> HandleAsync(TCommand command);
}

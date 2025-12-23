namespace Application.Command;

public interface ICommandDispatcher
{
    Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command)
        where TCommand : ICommandRequest
        where TResponse : ICommandResponse;
}

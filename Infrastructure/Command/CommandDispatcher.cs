using Application.Command;
using Application.Exception;

namespace Infrastructure.Command;

public class CommandDispatcher(
    IServiceProvider serviceProvider,
    ILogger<CommandDispatcher> logger
) : ICommandDispatcher
{
    public async Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command)
        where TCommand : ICommand
        where TResponse : ICommandResponse
    {
        logger.LogInformation("Dispatching {CommandName}", typeof(TCommand).Name);

        var handlerType = typeof(ICommandHandler<TCommand, TResponse>);
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            throw new InternalSystemMisconfiguredException($"Handler is not found for {command.GetType().Name}");
        }

        return await ((ICommandHandler<TCommand, TResponse>)handler).HandleAsync(command);
    }
}

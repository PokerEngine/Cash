using Application.Command;

namespace Infrastructure.Command;

public class CommandDispatcher(
    IServiceProvider serviceProvider,
    ILogger<CommandDispatcher> logger
) : ICommandDispatcher
{
    public async Task<TResult> DispatchAsync<TCommand, TResult>(TCommand command)
    {
        logger.LogInformation($"Dispatching command {typeof(TCommand).Name}");

        var handlerType = typeof(ICommandHandler<TCommand, TResult>);
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            throw new InvalidOperationException($"No handler found for command {typeof(TCommand).Name}");
        }

        return await ((ICommandHandler<TCommand, TResult>)handler).HandleAsync(command);
    }
}

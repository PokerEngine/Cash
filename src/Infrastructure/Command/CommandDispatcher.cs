using Application.Command;

namespace Infrastructure.Command;

public class CommandDispatcher(
    IServiceProvider serviceProvider,
    ILogger<CommandDispatcher> logger
)
{
    public async Task<TResponse> DispatchAsync<TCommand, TResponse>(TCommand command)
        where TCommand : ICommandRequest
        where TResponse : ICommandResponse
    {
        logger.LogInformation($"Dispatching command {typeof(TCommand).Name}");

        var handlerType = typeof(ICommandHandler<TCommand, TResponse>);
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            throw new InvalidOperationException($"No handler found for command {typeof(TCommand).Name}");
        }

        return await ((ICommandHandler<TCommand, TResponse>)handler).HandleAsync(command);
    }
}

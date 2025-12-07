using Application.Command;

namespace Infrastructure.Command;

public class CommandDispatcher(
    IServiceProvider serviceProvider,
    ILogger<CommandDispatcher> logger
)
{
    public async Task<TCommandResponse> DispatchAsync<TCommandRequest, TCommandResponse>(TCommandRequest command)
        where TCommandRequest : ICommandRequest
        where TCommandResponse : ICommandResponse
    {
        logger.LogInformation($"Dispatching command {typeof(TCommandRequest).Name}");

        var handlerType = typeof(ICommandHandler<TCommandRequest, TCommandResponse>);
        var handler = serviceProvider.GetService(handlerType);

        if (handler is null)
        {
            throw new InvalidOperationException($"No handler found for command {typeof(TCommandRequest).Name}");
        }

        return await ((ICommandHandler<TCommandRequest, TCommandResponse>)handler).HandleAsync(command);
    }
}

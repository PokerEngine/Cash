namespace Application.Command;

public interface ICommandRequest;

public interface ICommandResponse;

public interface ICommandHandler<in TCommandRequest, TCommandResponse>
    where TCommandRequest : ICommandRequest
    where TCommandResponse : ICommandResponse
{
    Task<TCommandResponse> HandleAsync(TCommandRequest command);
}

using Application.Repository;
using Application.Service.Hand;
using Domain.Entity;

namespace Application.Command;

public record SubmitPlayerActionCommand : ICommand
{
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
    public required string Type { get; init; }
    public required int Amount { get; init; }
}

public record SubmitPlayerActionResponse : ICommandResponse
{
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
}

public class SubmitPlayerActionHandler(
    IRepository repository,
    IHandService handService
) : ICommandHandler<SubmitPlayerActionCommand, SubmitPlayerActionResponse>
{
    public async Task<SubmitPlayerActionResponse> HandleAsync(SubmitPlayerActionCommand command)
    {
        var table = Table.FromEvents(
            uid: command.Uid,
            events: await repository.GetEventsAsync(command.Uid)
        );

        var type = (PlayerActionType)Enum.Parse(typeof(PlayerActionType), command.Type);
        await handService.SubmitPlayerActionAsync(
            handUid: table.GetCurrentHandUid(),
            nickname: command.Nickname,
            type: type,
            amount: command.Amount
        );

        return new SubmitPlayerActionResponse
        {
            Uid = table.Uid,
            Nickname = command.Nickname
        };
    }
}

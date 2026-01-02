using Application.Repository;
using Application.Service.Hand;
using Domain.Entity;

namespace Application.Command;

public record struct CommitDecisionCommand : ICommand
{
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
    public required string Type { get; init; }
    public required int Amount { get; init; }
}

public record struct CommitDecisionResponse : ICommandResponse
{
    public required Guid Uid { get; init; }
    public required string Nickname { get; init; }
}

public class CommitDecisionHandler(
    IRepository repository,
    IHandService handService
) : ICommandHandler<CommitDecisionCommand, CommitDecisionResponse>
{
    public async Task<CommitDecisionResponse> HandleAsync(CommitDecisionCommand command)
    {
        var table = Table.FromEvents(
            uid: command.Uid,
            events: await repository.GetEventsAsync(command.Uid)
        );

        var type = (DecisionType)Enum.Parse(typeof(DecisionType), command.Type);
        await handService.CommitDecisionAsync(
            handUid: table.GetHandUid(),
            nickname: command.Nickname,
            type: type,
            amount: command.Amount
        );

        return new CommitDecisionResponse
        {
            Uid = table.Uid,
            Nickname = command.Nickname
        };
    }
}

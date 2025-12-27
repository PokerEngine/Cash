using Application.Repository;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.Query;

public record struct GetTableByUidQuery : IQuery
{
    public required Guid Uid { get; init; }
}

public record struct GetTableByUidResponse : IQueryResponse
{
    public required Guid Uid { get; init; }
    public required List<Participant> Participants { get; init; }
}

public class GetTableByUidHandler(
    IRepository repository
) : IQueryHandler<GetTableByUidQuery, GetTableByUidResponse>
{
    public async Task<GetTableByUidResponse> HandleAsync(GetTableByUidQuery command)
    {
        var table = Table.FromEvents(
            uid: command.Uid,
            events: await repository.GetEventsAsync(command.Uid)
        );

        return new GetTableByUidResponse
        {
            Uid = table.Uid,
            Participants = table.GetParticipants().ToList()
        };
    }
}

using Application.Repository;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.Query;

public record struct GetTableByUidQuery : IQueryRequest
{
    public required Guid Uid;
}

public record struct GetTableByUidResponse : IQueryResponse
{
    public required Guid Uid;
    public required List<Participant> Participants;
}

public class GetTableByUidHandler(
    IRepository repository
) : IQueryHandler<GetTableByUidQuery, GetTableByUidResponse>
{
    public async Task<GetTableByUidResponse> HandleAsync(GetTableByUidQuery command)
    {
        var table = Table.FromEvents(
            events: await repository.GetEventsAsync(command.Uid)
        );

        return new GetTableByUidResponse
        {
            Uid = table.Uid,
            Participants = table.GetParticipants().ToList()
        };
    }
}

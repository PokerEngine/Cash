using Application.Repository;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.Query;

public record GetTableByUidQuery(
    Guid TableUid
);

public record GetTableByUidResponse(
    Guid TableUid,
    List<Participant> Participants
);

public class GetTableByUidHandler(
    IRepository repository
) : IQueryHandler<GetTableByUidQuery, GetTableByUidResponse>
{
    public async Task<GetTableByUidResponse> HandleAsync(GetTableByUidQuery command)
    {
        var table = Table.FromEvents(
            events: await repository.GetEventsAsync(command.TableUid)
        );

        return new GetTableByUidResponse(
            TableUid: table.Uid,
            Participants: table.GetParticipants().ToList()
        );
    }
}

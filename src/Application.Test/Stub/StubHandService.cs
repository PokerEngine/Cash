using Application.Service;
using Domain.ValueObject;

namespace Application.Test.Stub;

public class StubHandService : IHandService
{
    public async Task<HandState> CreateHandAsync(
        TableUid tableUid,
        Game game,
        Seat maxSeat,
        Chips smallBlind,
        Chips bigBlind,
        Seat? smallBlindSeat,
        Seat bigBlindSeat,
        Seat buttonSeat,
        IEnumerable<Participant> participants,
        CancellationToken cancellationToken = default
    )
    {
        await Task.CompletedTask;

        return new HandState(
            Uid: new HandUid(Guid.NewGuid()),
            Participants: participants as IReadOnlyList<Participant>
        );
    }
}

using Domain.ValueObject;

namespace Application.Service;

public record HandState(
    HandUid Uid,
    IReadOnlyList<Participant> Participants
);

public interface IHandService
{
    Task<HandState> CreateHandAsync(
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
    );
}

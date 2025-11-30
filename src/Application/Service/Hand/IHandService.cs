using Domain.ValueObject;

namespace Application.Service.Hand;

public record struct HandState(
    HandUid HandUid,
    List<Participant> Participants
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

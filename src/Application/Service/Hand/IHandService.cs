using Domain.ValueObject;

namespace Application.Service.Hand;

public record struct HandState(
    HandUid HandUid,
    List<Participant> Participants
);

public interface IHandService
{
    Task<HandState> GetAsync(
        HandUid handUid,
        CancellationToken cancellationToken = default
    );

    Task<HandState> CreateAsync(
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

    Task<HandState> StartAsync(
        HandUid handUid,
        CancellationToken cancellationToken = default
    );

    Task<HandState> CommitDecisionAsync(
        HandUid handUid,
        Nickname nickname,
        Decision decision,
        CancellationToken cancellationToken = default
    );
}

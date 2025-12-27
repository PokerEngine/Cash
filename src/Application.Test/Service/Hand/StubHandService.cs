using Application.Service.Hand;
using Domain.ValueObject;

namespace Application.Test.Service.Hand;

public class StubHandService : IHandService
{
    private Dictionary<HandUid, List<Participant>> _hands = new();

    public async Task<HandState> GetAsync(HandUid handUid, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;

        if (_hands.TryGetValue(handUid, out var participants))
        {
            return new HandState(
                HandUid: handUid,
                Participants: participants
            );
        }

        throw new InvalidOperationException("The hand is not found");
    }

    public async Task<HandState> CreateAsync(
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

        var handUid = new HandUid(Guid.NewGuid());
        _hands[handUid] = participants.ToList();

        return new HandState(
            HandUid: handUid,
            Participants: _hands[handUid]
        );
    }

    public async Task<HandState> StartAsync(
        HandUid handUid,
        CancellationToken cancellationToken = default
    )
    {
        return await GetAsync(handUid, cancellationToken);
    }

    public async Task<HandState> CommitDecisionAsync(
        HandUid handUid,
        Nickname nickname,
        Decision decision,
        CancellationToken cancellationToken = default
    )
    {
        return await GetAsync(handUid, cancellationToken);
    }
}

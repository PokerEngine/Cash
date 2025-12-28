using Application.Service.Hand;
using Domain.ValueObject;
using System.Collections.Concurrent;

namespace Infrastructure.Service.Hand;

public class InMemoryHandService : IHandService
{
    private readonly ConcurrentDictionary<HandUid, HandState> _mapping = new();

    public Task<HandState> GetAsync(
        HandUid handUid,
        CancellationToken cancellationToken = default
    )
    {
        if (!_mapping.TryGetValue(handUid, out var state))
        {
            throw new InvalidOperationException("The hand is not found");
        }

        return Task.FromResult(state);
    }

    public Task<HandState> CreateAsync(
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
        var state = new HandState(
            HandUid: new HandUid(Guid.NewGuid()),
            Participants: participants.ToList()
        );
        _mapping.TryAdd(state.HandUid, state);

        return GetAsync(state.HandUid, cancellationToken);
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

using Application.Service.Hand;
using Domain.ValueObject;
using System.Collections.Concurrent;

namespace Application.Test.Service.Hand;

public class StubHandService : IHandService
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

    public Task<HandUid> CreateAsync(
        TableUid tableUid,
        Game game,
        Seat maxSeat,
        Chips smallBlind,
        Chips bigBlind,
        Seat? smallBlindSeat,
        Seat bigBlindSeat,
        Seat buttonSeat,
        List<HandParticipant> participants,
        CancellationToken cancellationToken = default
    )
    {
        var state = new HandState
        {
            HandUid = new HandUid(Guid.NewGuid()),
            Players = participants.Select(p => new HandStatePlayer
            {
                Nickname = p.Nickname,
                Seat = p.Seat,
                Stack = p.Stack,
                HoleCards = [],
                IsFolded = false
            }).ToList(),
            BoardCards = [],
            Pot = new HandStatePot
            {
                DeadAmount = new Chips(0),
                Contributions = []
            },
            Bets = []
        };
        _mapping.TryAdd(state.HandUid, state);

        return Task.FromResult(state.HandUid);
    }

    public Task StartAsync(
        HandUid handUid,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }

    public Task CommitDecisionAsync(
        HandUid handUid,
        Nickname nickname,
        DecisionType type,
        Chips amount,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }
}

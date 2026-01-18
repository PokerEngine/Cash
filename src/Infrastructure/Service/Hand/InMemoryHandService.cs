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
        var handUid = new HandUid(Guid.NewGuid());
        var state = new HandState
        {
            Table = new HandStateTable
            {
                Players = participants.Select(CreatePlayer).ToList(),
                BoardCards = ""
            },
            Pot = new HandStatePot
            {
                Ante = 0,
                CommittedBets = [],
                UncommittedBets = [],
                Awards = []
            }
        };
        _mapping.TryAdd(handUid, state);

        return Task.FromResult(handUid);
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

    private HandStatePlayer CreatePlayer(HandParticipant participant)
    {
        return new HandStatePlayer
        {
            Nickname = participant.Nickname,
            Seat = participant.Seat,
            Stack = participant.Stack,
            HoleCards = "",
            IsFolded = false
        };
    }
}

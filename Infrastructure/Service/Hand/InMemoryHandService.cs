using Application.Exception;
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
            throw new ExternalSystemErrorException("The hand is not found");
        }

        return Task.FromResult(state);
    }

    public Task<HandUid> StartAsync(
        TableUid tableUid,
        HandRules rules,
        HandTable table,
        CancellationToken cancellationToken = default
    )
    {
        var handUid = new HandUid(Guid.NewGuid());
        var state = new HandState
        {
            Uid = handUid,
            TableUid = tableUid,
            Rules = rules,
            Table = table,
            Pot = new HandPot
            {
                Ante = 0,
                CollectedBets = [],
                CurrentBets = [],
                Awards = []
            }
        };
        _mapping.TryAdd(handUid, state);

        return Task.FromResult(handUid);
    }

    public Task SubmitPlayerActionAsync(
        HandUid handUid,
        Nickname nickname,
        PlayerActionType type,
        Chips amount,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }
}

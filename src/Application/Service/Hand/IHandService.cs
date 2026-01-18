using Domain.ValueObject;

namespace Application.Service.Hand;

public interface IHandService
{
    Task<HandState> GetAsync(
        HandUid handUid,
        CancellationToken cancellationToken = default
    );

    Task<HandUid> CreateAsync(
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
    );

    Task StartAsync(
        HandUid handUid,
        CancellationToken cancellationToken = default
    );

    Task CommitDecisionAsync(
        HandUid handUid,
        Nickname nickname,
        DecisionType type,
        Chips amount,
        CancellationToken cancellationToken = default
    );
}

public record struct HandParticipant
{
    public required Nickname Nickname { get; init; }
    public required Seat Seat { get; init; }
    public required Chips Stack { get; init; }
}

public record struct HandState
{
    public required HandStateTable Table { get; init; }
    public required HandStatePot Pot { get; init; }
}

public readonly struct HandStateTable
{
    public required List<HandStatePlayer> Players { get; init; }
    public required string BoardCards { get; init; }
}

public readonly struct HandStatePlayer
{
    public required Nickname Nickname { get; init; }
    public required Seat Seat { get; init; }
    public required Chips Stack { get; init; }
    public required string HoleCards { get; init; }
    public required bool IsFolded { get; init; }
}

public readonly struct HandStatePot
{
    public required Chips Ante { get; init; }
    public required List<HandStateBet> CommittedBets { get; init; }
    public required List<HandStateBet> UncommittedBets { get; init; }
    public required List<HandStateAward> Awards { get; init; }
}

public readonly struct HandStateBet
{
    public required Nickname Nickname { get; init; }
    public required Chips Amount { get; init; }
}

public readonly struct HandStateAward
{
    public required List<Nickname> Nicknames { get; init; }
    public required Chips Amount { get; init; }
}

public enum DecisionType
{
    Fold,
    Check,
    Call,
    RaiseTo
}

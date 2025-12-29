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
        IEnumerable<Participant> participants,
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

public record struct HandState
{
    public required HandUid HandUid { get; init; }
    public required IReadOnlyList<HandStatePlayer> Players { get; init; }
    public required IReadOnlyList<HandStateCard> BoardCards { get; init; }
    public required HandStatePot Pot { get; init; }
    public required IReadOnlyList<HandStateBet> Bets { get; init; }
}

public record struct HandStatePlayer
{
    public required Nickname Nickname { get; init; }
    public required Seat Seat { get; init; }
    public required Chips Stack { get; init; }
    public required IReadOnlyList<HandStateCard> HoleCards { get; init; }
    public required bool IsFolded { get; init; }
}

public record struct HandStateCard(string Value)
{
    public static implicit operator HandStateCard(string value) => new(value);
    public static implicit operator string(HandStateCard card) => card.ToString();
}

public record struct HandStatePot
{
    public required Chips DeadAmount { get; init; }
    public required IReadOnlyList<HandStateBet> Contributions { get; init; }
}

public record struct HandStateBet
{
    public required Nickname Nickname { get; init; }
    public required Chips Amount { get; init; }
}

public enum DecisionType
{
    Fold,
    Check,
    CallTo,
    RaiseTo
}

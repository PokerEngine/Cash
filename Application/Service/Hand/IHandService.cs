using Domain.ValueObject;

namespace Application.Service.Hand;

public interface IHandService
{
    Task<HandState> GetAsync(
        HandUid handUid,
        CancellationToken cancellationToken = default
    );

    Task<HandUid> StartAsync(
        TableUid tableUid,
        HandRules rules,
        HandTable table,
        CancellationToken cancellationToken = default
    );

    Task SubmitPlayerActionAsync(
        HandUid handUid,
        Nickname nickname,
        PlayerActionType type,
        Chips amount,
        CancellationToken cancellationToken = default
    );
}

public record HandState
{
    public required HandUid Uid { get; init; }
    public required TableUid TableUid { get; init; }
    public required HandRules Rules { get; init; }
    public required HandTable Table { get; init; }
    public required HandPot Pot { get; init; }
}

public record HandRules
{
    public required Game Game { get; init; }
    public required Seat MaxSeat { get; init; }
    public required Chips SmallBlind { get; init; }
    public required Chips BigBlind { get; init; }
}

public record HandTable
{
    public required HandPositions Positions { get; init; }
    public required List<HandPlayer> Players { get; init; }
    public string BoardCards { get; init; } = "";
}

public record HandPositions
{
    public required Seat? SmallBlindSeat { get; init; }
    public required Seat BigBlindSeat { get; init; }
    public required Seat ButtonSeat { get; init; }
}

public record HandPlayer
{
    public required Nickname Nickname { get; init; }
    public required Seat Seat { get; init; }
    public required Chips Stack { get; init; }
    public string HoleCards { get; init; } = "";
    public bool IsFolded { get; init; } = false;
}

public record HandPot
{
    public required Chips Ante { get; init; }
    public required List<HandBet> CollectedBets { get; init; }
    public required List<HandBet> CurrentBets { get; init; }
    public required List<HandAward> Awards { get; init; }
}

public record HandBet
{
    public required Nickname Nickname { get; init; }
    public required Chips Amount { get; init; }
}

public record HandAward
{
    public required List<Nickname> Winners { get; init; }
    public required Chips Amount { get; init; }
}

public enum PlayerActionType
{
    Fold,
    Check,
    CallBy,
    RaiseBy
}

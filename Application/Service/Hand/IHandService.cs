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

    Task SubmitPlayerActionAsync(
        HandUid handUid,
        Nickname nickname,
        PlayerActionType type,
        Chips amount,
        CancellationToken cancellationToken = default
    );
}

public record HandParticipant
{
    public required Nickname Nickname { get; init; }
    public required Seat Seat { get; init; }
    public required Chips Stack { get; init; }
}

public record HandState
{
    public required HandStateTable Table { get; init; }
    public required HandStatePot Pot { get; init; }
}

public record HandStateTable
{
    public required List<HandStatePlayer> Players { get; init; }
    public required string BoardCards { get; init; }
}

public record HandStatePlayer
{
    public required Nickname Nickname { get; init; }
    public required Seat Seat { get; init; }
    public required Chips Stack { get; init; }
    public required string HoleCards { get; init; }
    public required bool IsFolded { get; init; }
}

public record HandStatePot
{
    public required Chips Ante { get; init; }
    public required List<HandStateBet> CollectedBets { get; init; }
    public required List<HandStateBet> CurrentBets { get; init; }
    public required List<HandStateAward> Awards { get; init; }
}

public record HandStateBet
{
    public required Nickname Nickname { get; init; }
    public required Chips Amount { get; init; }
}

public record HandStateAward
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

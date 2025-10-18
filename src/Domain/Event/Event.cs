using Domain.ValueObject;

namespace Domain.Event;

public abstract record BaseEvent(
    DateTime OccuredAt
);

public record TableIsCreatedEvent(
    TableUid Uid,
    Game Game,
    Chips SmallBlind,
    Chips BigBlind,
    Money ChipCost,
    Seat MaxSeat,
    DateTime OccuredAt
) : BaseEvent(OccuredAt);

public record PlayerSatDownEvent(
    Nickname Nickname,
    Seat Seat,
    Chips Stack,
    bool IsWaitingForBigBlind,
    DateTime OccuredAt
) : BaseEvent(OccuredAt);

public record PlayerStoodUpEvent(
    Nickname Nickname,
    DateTime OccuredAt
) : BaseEvent(OccuredAt);

public record PlayerSatOutEvent(
    Nickname Nickname,
    DateTime OccuredAt
) : BaseEvent(OccuredAt);

public record PlayerSatInEvent(
    Nickname Nickname,
    bool IsWaitingForBigBlind,
    DateTime OccuredAt
) : BaseEvent(OccuredAt);

public record HandIsStartedEvent(
    HandUid HandUid,
    DateTime OccuredAt
) : BaseEvent(OccuredAt);

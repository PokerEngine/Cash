using Domain.ValueObject;

namespace Domain.Event;

public abstract record BaseEvent(
    DateTime OccuredAt
);

public record TableIsCreatedEvent(
    TableUid Uid,
    Game Game,
    Seat MaxSeat,
    Chips SmallBlind,
    Chips BigBlind,
    Money ChipCost,
    DateTime OccuredAt
) : BaseEvent(OccuredAt);

public record PlayerSatDownEvent(
    Nickname Nickname,
    Seat Seat,
    Chips Stack,
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
    DateTime OccuredAt
) : BaseEvent(OccuredAt);

public record ButtonIsRotatedEvent(
    DateTime OccuredAt
) : BaseEvent(OccuredAt);

public record HandIsStartedEvent(
    HandUid HandUid,
    DateTime OccuredAt
) : BaseEvent(OccuredAt);

public record HandIsFinishedEvent(
    HandUid HandUid,
    DateTime OccuredAt
) : BaseEvent(OccuredAt);

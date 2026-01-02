using Domain.ValueObject;

namespace Domain.Event;

public interface IEvent
{
    DateTime OccurredAt { init; get; }
}

public record struct TableIsCreatedEvent : IEvent
{
    public required Game Game { get; init; }
    public required Seat MaxSeat { get; init; }
    public required Chips SmallBlind { get; init; }
    public required Chips BigBlind { get; init; }
    public required Money ChipCost { get; init; }
    public required DateTime OccurredAt { get; init; }
}

public record struct PlayerSatDownEvent : IEvent
{
    public required Nickname Nickname { get; init; }
    public required Seat Seat { get; init; }
    public required Chips Stack { get; init; }
    public required DateTime OccurredAt { get; init; }
}

public record struct PlayerStoodUpEvent : IEvent
{
    public required Nickname Nickname { get; init; }
    public required DateTime OccurredAt { get; init; }
}

public record struct PlayerSatOutEvent : IEvent
{
    public required Nickname Nickname { get; init; }
    public required DateTime OccurredAt { get; init; }
}

public record struct PlayerSatInEvent : IEvent
{
    public required Nickname Nickname { get; init; }
    public required DateTime OccurredAt { get; init; }
}

public record struct ButtonIsRotatedEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }
}

public record struct CurrentHandIsSetEvent : IEvent
{
    public required HandUid HandUid { get; init; }
    public required DateTime OccurredAt { get; init; }
}

public record struct CurrentHandIsClearedEvent : IEvent
{
    public required HandUid HandUid { get; init; }
    public required DateTime OccurredAt { get; init; }
}

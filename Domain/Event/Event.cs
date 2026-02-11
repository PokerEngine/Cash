using Domain.ValueObject;

namespace Domain.Event;

public interface IEvent
{
    DateTime OccurredAt { init; get; }
}

public sealed record TableCreatedEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }

    public required Rules Rules { get; init; }
}

public sealed record PlayerSatDownEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }

    public required Nickname Nickname { get; init; }
    public required Seat Seat { get; init; }
    public required Chips Stack { get; init; }
}

public sealed record PlayerStoodUpEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }

    public required Nickname Nickname { get; init; }
}

public sealed record PlayerSatOutEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }

    public required Nickname Nickname { get; init; }
}

public sealed record PlayerSatInEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }

    public required Nickname Nickname { get; init; }
}

public sealed record PlayerChipsDebitedEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }

    public required Nickname Nickname { get; init; }
    public required Chips Amount { get; init; }
}

public sealed record PlayerChipsCreditedEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }

    public required Nickname Nickname { get; init; }
    public required Chips Amount { get; init; }
}

public sealed record ButtonRotatedEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }
}

public sealed record CurrentHandStartedEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }

    public required HandUid HandUid { get; init; }
}

public sealed record CurrentHandFinishedEvent : IEvent
{
    public required DateTime OccurredAt { get; init; }

    public required HandUid HandUid { get; init; }
}

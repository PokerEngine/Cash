using Domain.ValueObject;

namespace Application.Event;

public record struct EventContext
{
    public required TableUid TableUid { get; init; }
}

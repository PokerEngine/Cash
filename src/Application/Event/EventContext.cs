using Domain.ValueObject;

namespace Application.Event;

public record EventContext
{
    public required TableUid TableUid { get; init; }
}

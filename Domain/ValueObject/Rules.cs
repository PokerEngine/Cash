namespace Domain.ValueObject;

public record Rules
{
    public required Game Game { get; init; }
    public required Seat MaxSeat { get; init; }
    public required Chips SmallBlind { get; init; }
    public required Chips BigBlind { get; init; }
    public required Money ChipCost { get; init; }
}

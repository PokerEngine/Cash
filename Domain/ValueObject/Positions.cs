namespace Domain.ValueObject;

public record Positions
{
    public required Seat? SmallBlindSeat { get; init; }
    public required Seat BigBlindSeat { get; init; }
    public required Seat ButtonSeat { get; init; }
}

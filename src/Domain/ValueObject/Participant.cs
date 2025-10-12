namespace Domain.ValueObject;

public record Participant(
    Nickname Nickname,
    Chips Stack,
    Seat Seat
);

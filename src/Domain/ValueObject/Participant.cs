namespace Domain.ValueObject;

public record Participant(
    Nickname Nickname,
    Seat Seat,
    Chips Stack
);

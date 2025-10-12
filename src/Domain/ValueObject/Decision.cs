namespace Domain.ValueObject;

public enum DecisionType
{
    Fold,
    Check,
    CallTo,
    RaiseTo
}

public record Decision(
    Nickname Nickname,
    DecisionType Type,
    Chips Amount
);

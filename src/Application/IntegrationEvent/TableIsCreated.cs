namespace Application.IntegrationEvent;

public record TableIsCreatedIntegrationEvent(
    Guid TableUid,
    string Game,
    int MaxSeat,
    int SmallBlind,
    int BigBlind,
    decimal ChipCostAmount,
    string ChipCostCurrency,
    DateTime OccuredAt
) : IIntegrationEvent;

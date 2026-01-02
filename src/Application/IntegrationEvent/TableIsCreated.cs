namespace Application.IntegrationEvent;

public record struct TableIsCreatedIntegrationEvent : IIntegrationEvent
{
    public required Guid TableUid { get; init; }
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required int SmallBlind { get; init; }
    public required int BigBlind { get; init; }
    public required decimal ChipCostAmount { get; init; }
    public required string ChipCostCurrency { get; init; }
    public required DateTime OccurredAt { get; init; }
}

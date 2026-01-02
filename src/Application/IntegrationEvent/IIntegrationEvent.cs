namespace Application.IntegrationEvent;

public interface IIntegrationEvent
{
    Guid TableUid { init; get; }
    DateTime OccurredAt { init; get; }
}

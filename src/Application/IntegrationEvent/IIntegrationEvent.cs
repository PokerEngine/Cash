namespace Application.IntegrationEvent;

public interface IIntegrationEvent
{
    Guid TableUid { init; get; }
    DateTime OccuredAt { init; get; }
}

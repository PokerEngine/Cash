namespace Application.IntegrationEvent;

public interface IIntegrationEvent
{
    Guid Uid { init; get; }
    Guid? CorrelationUid { init; get; }
    DateTime OccurredAt { init; get; }

    Guid TableUid { init; get; }
}

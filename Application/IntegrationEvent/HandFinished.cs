using Application.Connection;
using Application.Repository;
using Application.Service.HandManager;
using Application.UnitOfWork;
using Domain.Entity;

namespace Application.IntegrationEvent;

public record HandFinishedIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
}

public class HandFinishedHandler(
    IConnectionRegistry connectionRegistry,
    IRepository repository,
    IHandManager handManager,
    IUnitOfWork unitOfWork
) : IIntegrationEventHandler<HandFinishedIntegrationEvent>
{
    public async Task HandleAsync(HandFinishedIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );

        var events = await repository.GetEventsAsync(integrationEvent.TableUid);
        var table = Table.FromEvents(integrationEvent.TableUid, events);

        table.FinishCurrentHand(integrationEvent.HandUid);

        if (table.HasEnoughPlayersForHand())
        {
            await handManager.StartHandAsync(table);
        }

        unitOfWork.RegisterTable(table);
        await unitOfWork.CommitAsync();
    }
}

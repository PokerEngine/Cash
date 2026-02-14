using Application.Connection;
using Application.Repository;
using Application.UnitOfWork;
using Domain.Entity;

namespace Application.IntegrationEvent;

public record BetRefundedIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
    public required string Nickname { get; init; }
    public required int Amount { get; init; }
}

public class BetRefundedHandler(
    IConnectionRegistry connectionRegistry,
    IRepository repository,
    IUnitOfWork unitOfWork
) : IIntegrationEventHandler<BetRefundedIntegrationEvent>
{
    public async Task HandleAsync(BetRefundedIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );

        if (integrationEvent.Amount > 0)
        {
            var events = await repository.GetEventsAsync(integrationEvent.TableUid);
            var table = Table.FromEvents(integrationEvent.TableUid, events);

            table.CreditPlayerChips(integrationEvent.Nickname, integrationEvent.Amount);

            unitOfWork.RegisterTable(table);
            await unitOfWork.CommitAsync();
        }
    }
}

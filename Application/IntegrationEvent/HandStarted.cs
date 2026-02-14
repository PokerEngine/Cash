using Application.Connection;
using Application.Repository;
using Application.UnitOfWork;
using Domain.Entity;

namespace Application.IntegrationEvent;

public record IntegrationEventPlayer
{
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
}

public record HandStartedIntegrationEvent : IIntegrationEvent
{
    public required Guid Uid { init; get; }
    public Guid? CorrelationUid { init; get; }
    public required DateTime OccurredAt { get; init; }

    public required Guid TableUid { get; init; }

    public required Guid HandUid { get; init; }
    public required string Game { get; init; }
    public required int SmallBlind { get; init; }
    public required int BigBlind { get; init; }
    public required int MaxSeat { get; init; }
    public required int SmallBlindSeat { get; init; }
    public required int BigBlindSeat { get; init; }
    public required int ButtonSeat { get; init; }
    public required List<IntegrationEventPlayer> Players { get; init; }
}

public class HandStartedHandler(
    IConnectionRegistry connectionRegistry,
    IRepository repository,
    IUnitOfWork unitOfWork
) : IIntegrationEventHandler<HandStartedIntegrationEvent>
{
    public async Task HandleAsync(HandStartedIntegrationEvent integrationEvent)
    {
        await connectionRegistry.SendIntegrationEventToTableAsync(
            tableUid: integrationEvent.TableUid,
            integrationEvent: integrationEvent
        );

        var events = await repository.GetEventsAsync(integrationEvent.TableUid);
        var table = Table.FromEvents(integrationEvent.TableUid, events);

        table.StartCurrentHand(integrationEvent.HandUid);

        unitOfWork.RegisterTable(table);
        await unitOfWork.CommitAsync();
    }
}

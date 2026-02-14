using Application.Command;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Storage;
using Application.Test.UnitOfWork;
using Domain.Entity;
using Domain.Event;
using Domain.ValueObject;

namespace Application.Test.Command;

public class CreateTableTest
{
    [Fact]
    public async Task HandleAsync_Valid_ShouldCreateTable()
    {
        // Arrange
        var unitOfWork = CreateUnitOfWork();
        var command = new CreateTableCommand
        {
            Rules = new CreateTableCommandRules
            {
                Game = "NoLimitHoldem",
                MaxSeat = 6,
                SmallBlind = 5,
                BigBlind = 10,
                ChipCostAmount = 1,
                ChipCostCurrency = "Usd"
            }
        };
        var handler = new CreateTableHandler(unitOfWork.Repository, unitOfWork);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        var table = Table.FromEvents(response.Uid, await unitOfWork.Repository.GetEventsAsync(response.Uid));
        Assert.Equal(new TableUid(response.Uid), table.Uid);
        Assert.Equal(Game.NoLimitHoldem, table.Rules.Game);
        Assert.Equal(new Seat(6), table.Rules.MaxSeat);
        Assert.Equal(new Chips(5), table.Rules.SmallBlind);
        Assert.Equal(new Chips(10), table.Rules.BigBlind);
        Assert.Equal(new Money(1, Currency.Usd), table.Rules.ChipCost);

        var detailView = await unitOfWork.Storage.GetDetailViewAsync(table.Uid);
        Assert.Equal((Guid)table.Uid, detailView.Uid);

        var listViews = await unitOfWork.Storage.GetListViewsAsync();
        Assert.Single(listViews);
        Assert.Equal((Guid)table.Uid, listViews[0].Uid);

        var events = await unitOfWork.EventDispatcher.GetDispatchedEvents(response.Uid);
        Assert.Single(events);
        Assert.IsType<TableCreatedEvent>(events[0]);
    }

    private StubUnitOfWork CreateUnitOfWork()
    {
        var repository = new StubRepository();
        var storage = new StubStorage();
        var eventDispatcher = new StubEventDispatcher();
        return new StubUnitOfWork(repository, storage, eventDispatcher);
    }
}

using Application.Command;
using Application.Test.Event;
using Application.Test.Repository;
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
        var repository = new StubRepository();
        var eventDispatcher = new StubEventDispatcher();
        var command = new CreateTableCommand
        {
            Game = "NoLimitHoldem",
            MaxSeat = 6,
            SmallBlind = 5,
            BigBlind = 10,
            ChipCostAmount = 1,
            ChipCostCurrency = "Usd"
        };
        var handler = new CreateTableHandler(repository, eventDispatcher);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        var table = Table.FromEvents(response.Uid, await repository.GetEventsAsync(response.Uid));
        Assert.Equal(new TableUid(response.Uid), table.Uid);
        Assert.Equal(Game.NoLimitHoldem, table.Game);
        Assert.Equal(new Seat(6), table.MaxSeat);
        Assert.Equal(new Chips(5), table.SmallBlind);
        Assert.Equal(new Chips(10), table.BigBlind);
        Assert.Equal(new Money(1, Currency.Usd), table.ChipCost);

        var events = await eventDispatcher.GetDispatchedEvents(response.Uid);
        Assert.Single(events);
        Assert.IsType<TableCreatedEvent>(events[0]);
    }
}

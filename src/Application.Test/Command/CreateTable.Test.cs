using Application.Command;
using Application.Test.Stub;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.Test.Command;

public class CreateTableTest
{
    [Fact]
    public async Task HandleAsync_Valid_ShouldCreateTable()
    {
        // Arrange
        var repository = new StubRepository();
        var command = new CreateTableCommand
        {
            Game = "NoLimitHoldem",
            MaxSeat = 6,
            SmallBlind = 5,
            BigBlind = 10,
            ChipCostAmount = 1,
            ChipCostCurrency = "Usd"
        };
        var handler = new CreateTableHandler(repository: repository);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.Game, response.Game);
        Assert.Equal(command.MaxSeat, response.MaxSeat);
        Assert.Equal(command.SmallBlind, response.SmallBlind);
        Assert.Equal(command.BigBlind, response.BigBlind);
        Assert.Equal(command.ChipCostAmount, response.ChipCostAmount);
        Assert.Equal(command.ChipCostCurrency, response.ChipCostCurrency);

        var events = await repository.GetEventsAsync(response.Uid);
        var table = Table.FromEvents(events);
        Assert.Equal(new TableUid(response.Uid), table.Uid);
        Assert.Equal(Game.NoLimitHoldem, table.Game);
        Assert.Equal(new Seat(6), table.MaxSeat);
        Assert.Equal(new Chips(5), table.SmallBlind);
        Assert.Equal(new Chips(10), table.BigBlind);
        Assert.Equal(new Money(1, Currency.Usd), table.ChipCost);
    }
}

using Application.Command;
using Application.Test.Stub;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.Test.Command;

public class TableCreateHandlerTest
{
    [Fact]
    public async Task HandleAsync_Valid_ShouldCreateTable()
    {
        // Arrange
        var repository = new StubRepository();
        await repository.ConnectAsync();
        var command = new TableCreateCommand(
            Game: "NoLimitHoldem",
            MaxSeat: 6,
            SmallBlind: 5,
            BigBlind: 10,
            ChipCostAmount: 1,
            ChipCostCurrency: "Usd"
        );
        var handler = new TableCreateHandler(repository: repository);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.Game, result.Game);
        Assert.Equal(command.MaxSeat, result.MaxSeat);
        Assert.Equal(command.SmallBlind, result.SmallBlind);
        Assert.Equal(command.BigBlind, result.BigBlind);
        Assert.Equal(command.ChipCostAmount, result.ChipCostAmount);
        Assert.Equal(command.ChipCostCurrency, result.ChipCostCurrency);

        var events = await repository.GetEventsAsync(result.TableUid);
        var table = Table.FromEvents(events);
        Assert.Equal(new TableUid(result.TableUid), table.Uid);
        Assert.Equal(Game.NoLimitHoldem, table.Game);
        Assert.Equal(new Seat(6), table.MaxSeat);
        Assert.Equal(new Chips(5), table.SmallBlind);
        Assert.Equal(new Chips(10), table.BigBlind);
        Assert.Equal(new Money(1, Currency.Usd), table.ChipCost);
    }
}

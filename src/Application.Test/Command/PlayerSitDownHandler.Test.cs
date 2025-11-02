using Application.Command;
using Application.Test.Stub;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.Test.Command;

public class PlayerSitDownHandlerTest
{
    [Fact]
    public async Task HandleAsync_Valid_ShouldSitDown()
    {
        // Arrange
        var repository = new StubRepository();
        await repository.ConnectAsync();
        var tableUid = await CreateTableAsync(repository);

        var command = new PlayerSitDownCommand(
            TableUid: tableUid,
            Nickname: "Alice",
            Seat: 2,
            Stack: 1000
        );
        var handler = new PlayerSitDownHandler(repository: repository);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.TableUid, result.TableUid);
        Assert.Equal(command.Nickname, result.Nickname);
        Assert.Equal(command.Seat, result.Seat);
        Assert.Equal(command.Stack, result.Stack);

        var events = await repository.GetEventsAsync(result.TableUid);
        var table = Table.FromEvents(events);
        var player = table.Players.First();
        Assert.Equal(new Nickname("Alice"), player.Nickname);
        Assert.Equal(new Seat(2), player.Seat);
        Assert.Equal(new Chips(1000), player.Stack);
    }

    private async Task<Guid> CreateTableAsync(StubRepository repository)
    {
        var handler = new TableCreateHandler(repository: repository);
        var command = new TableCreateCommand(
            Game: "NoLimitHoldem",
            MaxSeat: 6,
            SmallBlind: 5,
            BigBlind: 10,
            ChipCostAmount: 1,
            ChipCostCurrency: "Usd"
        );
        var result = await handler.HandleAsync(command);
        return result.TableUid;
    }
}

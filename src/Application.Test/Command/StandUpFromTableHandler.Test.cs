using Application.Command;
using Application.Test.Stub;
using Domain.Entity;

namespace Application.Test.Command;

public class StandUpFromTableHandlerTest
{
    [Fact]
    public async Task HandleAsync_Valid_ShouldSitDown()
    {
        // Arrange
        var repository = new StubRepository();
        await repository.ConnectAsync();
        var tableUid = await CreateTableAsync(repository);
        await SitDownPlayerAsync(
            repository: repository,
            tableUid: tableUid,
            nickname: "Alice",
            seat: 2,
            stack: 1000
        );

        var command = new StandUpFromTableCommand(
            TableUid: tableUid,
            Nickname: "Alice"
        );
        var handler = new StandUpFromTableHandler(repository);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.TableUid, result.TableUid);
        Assert.Equal(command.Nickname, result.Nickname);

        var events = await repository.GetEventsAsync(result.TableUid);
        var table = Table.FromEvents(events);
        Assert.Empty(table.Players);
    }

    private async Task<Guid> CreateTableAsync(StubRepository repository)
    {
        var handler = new CreateTableHandler(repository: repository);
        var command = new CreateTableCommand(
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

    private async Task SitDownPlayerAsync(
        StubRepository repository,
        Guid tableUid,
        string nickname,
        int seat,
        int stack
    )
    {
        var handler = new SitDownAtTableHandler(
            repository: repository,
            handService: new StubHandService()
        );
        var command = new SitDownAtTableCommand(
            TableUid: tableUid,
            Nickname: nickname,
            Seat: seat,
            Stack: stack
        );
        await handler.HandleAsync(command);
    }
}

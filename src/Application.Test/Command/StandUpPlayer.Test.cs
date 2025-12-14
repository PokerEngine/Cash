using Application.Command;
using Application.Test.Stub;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.Test.Command;

public class StandUpPlayerTest
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

        var command = new StandUpPlayerCommand(
            TableUid: tableUid,
            Nickname: "Alice"
        );
        var handler = new StandUpPlayerHandler(repository);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.TableUid, response.TableUid);
        Assert.Equal(command.Nickname, response.Nickname);

        var events = await repository.GetEventsAsync(response.TableUid);
        var table = Table.FromEvents(events);
        Assert.Empty(table.Players);
    }

    [Fact]
    public async Task HandleAsync_NotExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var repository = new StubRepository();
        await repository.ConnectAsync();
        var handService = new StubHandService();

        var command = new StandUpPlayerCommand(
            TableUid: new TableUid(Guid.NewGuid()),
            Nickname: "Alice"
        );
        var handler = new StandUpPlayerHandler(repository);

        // Act
        var exc = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.HandleAsync(command);
        });

        // Assert
        Assert.Equal("The table is not found", exc.Message);
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
        var response = await handler.HandleAsync(command);
        return response.TableUid;
    }

    private async Task SitDownPlayerAsync(
        StubRepository repository,
        Guid tableUid,
        string nickname,
        int seat,
        int stack
    )
    {
        var handler = new SitDownPlayerHandler(
            repository: repository,
            handService: new StubHandService()
        );
        var command = new SitDownPlayerCommand(
            TableUid: tableUid,
            Nickname: nickname,
            Seat: seat,
            Stack: stack
        );
        await handler.HandleAsync(command);
    }
}

using Application.Command;
using Application.Test.Stub;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.Test.Command;

public class SitDownPlayerTest
{
    [Fact]
    public async Task HandleAsync_1stPlayer_ShouldSitDownAndWait()
    {
        // Arrange
        var repository = new StubRepository();
        var handService = new StubHandService();
        var tableUid = await CreateTableAsync(repository);

        var command = new SitDownPlayerCommand(
            TableUid: tableUid,
            Nickname: "Alice",
            Seat: 2,
            Stack: 1000
        );
        var handler = new SitDownPlayerHandler(
            repository: repository,
            handService: handService
        );

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.TableUid, response.TableUid);
        Assert.Equal(command.Nickname, response.Nickname);
        Assert.Equal(command.Seat, response.Seat);
        Assert.Equal(command.Stack, response.Stack);

        var events = await repository.GetEventsAsync(response.TableUid);
        var table = Table.FromEvents(events);
        Assert.False(table.IsHandInProgress());

        var player = table.Players.First();
        Assert.Equal(new Nickname("Alice"), player.Nickname);
        Assert.Equal(new Seat(2), player.Seat);
        Assert.Equal(new Chips(1000), player.Stack);
    }

    [Fact]
    public async Task HandleAsync_2ndPlayer_ShouldSitDownAndStartHand()
    {
        // Arrange
        var repository = new StubRepository();
        var handService = new StubHandService();
        var tableUid = await CreateTableAsync(repository);
        await SitDownPlayerAsync(
            repository: repository,
            handService: handService,
            tableUid: tableUid,
            nickname: "Alice",
            seat: 2,
            stack: 1000
        );

        var command = new SitDownPlayerCommand(
            TableUid: tableUid,
            Nickname: "Bob",
            Seat: 4,
            Stack: 1000
        );
        var handler = new SitDownPlayerHandler(
            repository: repository,
            handService: handService
        );

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.TableUid, response.TableUid);
        Assert.Equal(command.Nickname, response.Nickname);
        Assert.Equal(command.Seat, response.Seat);
        Assert.Equal(command.Stack, response.Stack);

        var events = await repository.GetEventsAsync(response.TableUid);
        var table = Table.FromEvents(events);
        Assert.True(table.IsHandInProgress());
        Assert.Equal(new Seat(2), table.ButtonSeat);
        Assert.Equal(new Seat(2), table.SmallBlindSeat);
        Assert.Equal(new Seat(4), table.BigBlindSeat);
    }

    [Fact]
    public async Task HandleAsync_NotExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var repository = new StubRepository();
        var handService = new StubHandService();

        var command = new SitDownPlayerCommand(
            TableUid: new TableUid(Guid.NewGuid()),
            Nickname: "Alice",
            Seat: 2,
            Stack: 1000
        );
        var handler = new SitDownPlayerHandler(
            repository: repository,
            handService: handService
        );

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
        StubHandService handService,
        Guid tableUid,
        string nickname,
        int seat,
        int stack
    )
    {
        var handler = new SitDownPlayerHandler(
            repository: repository,
            handService: handService
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

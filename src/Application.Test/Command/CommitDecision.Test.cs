using Application.Command;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Service.Hand;

namespace Application.Test.Command;

public class CommitDecisionTest
{
    [Fact]
    public async Task HandleAsync_Valid_ShouldCommitDecision()
    {
        // Arrange
        var repository = new StubRepository();
        var eventDispatcher = new StubEventDispatcher();
        var handService = new StubHandService();
        var tableUid = await CreateTableAsync(repository, eventDispatcher);
        await SitDownPlayerAsync(
            repository: repository,
            eventDispatcher: eventDispatcher,
            handService: handService,
            tableUid: tableUid,
            nickname: "Alice",
            seat: 2,
            stack: 1000
        );
        await SitDownPlayerAsync(
            repository: repository,
            eventDispatcher: eventDispatcher,
            handService: handService,
            tableUid: tableUid,
            nickname: "Bobby",
            seat: 4,
            stack: 1000
        );

        var command = new CommitDecisionCommand
        {
            Uid = tableUid,
            Nickname = "Bobby",
            Type = "RaiseTo",
            Amount = 25
        };
        var handler = new CommitDecisionHandler(repository, handService);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.Uid, response.Uid);
        Assert.Equal(command.Nickname, response.Nickname);

        // TODO: perform taking chips from player's stack and adding to pot, and test it
    }

    private async Task<Guid> CreateTableAsync(StubRepository repository, StubEventDispatcher eventDispatcher)
    {
        var handler = new CreateTableHandler(repository, eventDispatcher);
        var command = new CreateTableCommand
        {
            Game = "NoLimitHoldem",
            MaxSeat = 6,
            SmallBlind = 5,
            BigBlind = 10,
            ChipCostAmount = 1,
            ChipCostCurrency = "Usd"
        };
        var response = await handler.HandleAsync(command);
        return response.Uid;
    }

    private async Task SitDownPlayerAsync(
        StubRepository repository,
        StubEventDispatcher eventDispatcher,
        StubHandService handService,
        Guid tableUid,
        string nickname,
        int seat,
        int stack
    )
    {
        var handler = new SitDownPlayerHandler(repository, eventDispatcher, handService);
        var command = new SitDownPlayerCommand
        {
            Uid = tableUid,
            Nickname = nickname,
            Seat = seat,
            Stack = stack
        };
        await handler.HandleAsync(command);
    }
}

using Application.Command;
using Application.Query;
using Application.Test.Stub;

namespace Application.Test.Query;

public class GetTableByUidTest
{
    [Fact]
    public async Task HandleAsync_Exists_ShouldReturn()
    {
        // Arrange
        var repository = new StubRepository();
        var tableUid = await CreateTableAsync(repository);

        var query = new GetTableByUidQuery { Uid = tableUid };
        var handler = new GetTableByUidHandler(
            repository: repository
        );

        // Act
        var response = await handler.HandleAsync(query);

        // Assert
        Assert.Equal(query.Uid, response.Uid);
        Assert.Empty(response.Participants);
    }

    [Fact]
    public async Task HandleAsync_NotExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var repository = new StubRepository();

        var query = new GetTableByUidQuery { Uid = Guid.NewGuid() };
        var handler = new GetTableByUidHandler(
            repository: repository
        );

        // Act
        var exc = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await handler.HandleAsync(query);
        });

        // Assert
        Assert.Equal("The table is not found", exc.Message);
    }

    private async Task<Guid> CreateTableAsync(StubRepository repository)
    {
        var handler = new CreateTableHandler(repository: repository);
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
}

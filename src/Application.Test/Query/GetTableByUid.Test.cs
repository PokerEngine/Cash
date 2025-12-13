using Application.Command;
using Application.Query;
using Application.Test.Stub;
using Domain.ValueObject;

namespace Application.Test.Query;

public class GetTableByUidTest
{
    [Fact]
    public async Task HandleAsync_Exists_ShouldReturn()
    {
        // Arrange
        var repository = new StubRepository();
        await repository.ConnectAsync();
        var tableUid = await CreateTableAsync(repository);

        var query = new GetTableByUidQuery(
            TableUid: tableUid
        );
        var handler = new GetTableByUidHandler(
            repository: repository
        );

        // Act
        var response = await handler.HandleAsync(query);

        // Assert
        Assert.Equal(query.TableUid, response.TableUid);
        Assert.Empty(response.Participants);
    }

    [Fact]
    public async Task HandleAsync_NotExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var repository = new StubRepository();
        await repository.ConnectAsync();

        var query = new GetTableByUidQuery(
            TableUid: new TableUid(Guid.NewGuid())
        );
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
}

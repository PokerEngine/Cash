using Application.Command;
using Application.Service.Hand;
using Application.Test.Event;
using Application.Test.Repository;
using Application.Test.Service.Hand;
using Application.Test.Storage;
using Application.Test.UnitOfWork;
using Domain.Entity;

namespace Application.Test.Command;

public class SubmitPlayerActionTest
{
    [Fact]
    public async Task HandleAsync_Valid_ShouldSubmitPlayerAction()
    {
        // Arrange
        var unitOfWork = CreateUnitOfWork();
        var handService = new StubHandService();
        var tableUid = await CreateTableAsync(unitOfWork);
        await SitPlayerDownAsync(unitOfWork, tableUid, "Alice", 2, 1000);
        await SitPlayerDownAsync(unitOfWork, tableUid, "Bobby", 4, 1000);
        await StartHandAsync(unitOfWork, handService, tableUid);

        var command = new SubmitPlayerActionCommand
        {
            Uid = tableUid,
            Nickname = "Bobby",
            Type = "RaiseBy",
            Amount = 20
        };
        var handler = new SubmitPlayerActionHandler(unitOfWork.Repository, handService);

        // Act
        var response = await handler.HandleAsync(command);

        // Assert
        Assert.Equal(command.Uid, response.Uid);
        Assert.Equal(command.Nickname, response.Nickname);
    }

    private async Task<Guid> CreateTableAsync(StubUnitOfWork unitOfWork)
    {
        var handler = new CreateTableHandler(unitOfWork.Repository, unitOfWork);
        var command = new CreateTableCommand
        {
            Rules = new CreateTableCommandRules
            {
                Game = "NoLimitHoldem",
                MaxSeat = 6,
                SmallBlind = 5,
                BigBlind = 10,
                ChipCostAmount = 1,
                ChipCostCurrency = "Usd"
            }
        };
        var response = await handler.HandleAsync(command);
        await unitOfWork.EventDispatcher.ClearDispatchedEvents(response.Uid);
        return response.Uid;
    }

    private async Task SitPlayerDownAsync(
        StubUnitOfWork unitOfWork,
        Guid tableUid,
        string nickname,
        int seat,
        int stack
    )
    {
        var handler = new SitPlayerDownHandler(unitOfWork.Repository, unitOfWork);
        var command = new SitPlayerDownCommand
        {
            Uid = tableUid,
            Nickname = nickname,
            Seat = seat,
            Stack = stack
        };
        await handler.HandleAsync(command);
        await unitOfWork.EventDispatcher.ClearDispatchedEvents(tableUid);
    }

    private async Task StartHandAsync(
        StubUnitOfWork unitOfWork,
        StubHandService handService,
        Guid tableUid
    )
    {
        var table = Table.FromEvents(tableUid, await unitOfWork.Repository.GetEventsAsync(tableUid));
        table.RotateButton();

        var handUid = await handService.StartAsync(
            tableUid: table.Uid,
            rules: new HandRules
            {
                Game = table.Rules.Game,
                MaxSeat = table.Rules.MaxSeat,
                SmallBlind = table.Rules.SmallBlind,
                BigBlind = table.Rules.BigBlind
            },
            table: new HandTable
            {
                Positions = new HandPositions
                {
                    SmallBlindSeat = table.Positions!.SmallBlindSeat,
                    BigBlindSeat = table.Positions.BigBlindSeat,
                    ButtonSeat = table.Positions.ButtonSeat,
                },
                Players = table.ActivePlayers.Select(x => new HandPlayer
                {
                    Nickname = x.Nickname,
                    Seat = x.Seat,
                    Stack = x.Stack
                }).ToList()
            }
        );
        table.StartCurrentHand(handUid);

        unitOfWork.RegisterTable(table);
        await unitOfWork.CommitAsync();
    }

    private StubUnitOfWork CreateUnitOfWork()
    {
        var repository = new StubRepository();
        var storage = new StubStorage();
        var eventDispatcher = new StubEventDispatcher();
        return new StubUnitOfWork(repository, storage, eventDispatcher);
    }
}

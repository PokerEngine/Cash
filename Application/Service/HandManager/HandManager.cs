using Application.Service.Hand;
using Domain.Entity;
using Domain.ValueObject;

namespace Application.Service.HandManager;

public class HandManager(IHandService handService) : IHandManager
{
    public async Task StartHandAsync(Table table)
    {
        table.RotateButton();

        await handService.StartAsync(
            tableUid: table.Uid,
            rules: new HandRules
            {
                Game = table.Game,
                MaxSeat = table.MaxSeat,
                SmallBlind = table.SmallBlind,
                BigBlind = table.BigBlind,
            },
            table: new HandTable
            {
                Positions = new HandPositions
                {
                    SmallBlindSeat = table.SmallBlindSeat,
                    BigBlindSeat = (Seat)table.BigBlindSeat!,
                    ButtonSeat = (Seat)table.ButtonSeat!
                },
                Players = table.ActivePlayers.Select(GetPlayer).ToList()
            }
        );
    }

    public async Task SubmitPlayerActionAsync(Table table, Nickname nickname, PlayerActionType type, Chips amount)
    {
        await handService.SubmitPlayerActionAsync(
            handUid: table.GetCurrentHandUid(),
            nickname: nickname,
            type: type,
            amount: amount
        );
    }

    private HandPlayer GetPlayer(Player player)
    {
        return new HandPlayer
        {
            Nickname = player.Nickname,
            Seat = player.Seat,
            Stack = player.Stack
        };
    }
}

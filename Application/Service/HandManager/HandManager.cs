using Application.Service.Hand;
using Domain.Entity;

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
                Game = table.Rules.Game,
                MaxSeat = table.Rules.MaxSeat,
                SmallBlind = table.Rules.SmallBlind,
                BigBlind = table.Rules.BigBlind,
            },
            table: new HandTable
            {
                Positions = new HandPositions
                {
                    SmallBlindSeat = table.Positions!.SmallBlindSeat,
                    BigBlindSeat = table.Positions.BigBlindSeat,
                    ButtonSeat = table.Positions.ButtonSeat
                },
                Players = table.ActivePlayers.Select(GetPlayer).ToList()
            }
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

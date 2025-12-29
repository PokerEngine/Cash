using Application.Repository;
using Domain.Entity;

namespace Application.Query;

public record struct GetTableByUidQuery : IQuery
{
    public required Guid Uid { get; init; }
}

public record struct GetTableByUidResponse : IQueryResponse
{
    public required Guid Uid { get; init; }
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required int SmallBlind { get; init; }
    public required int BigBlind { get; init; }
    public required decimal ChipCostAmount { get; init; }
    public required string ChipCostCurrency { get; init; }
    public required List<GetTableByUidPlayerResponse> Players { get; init; }
}

public record struct GetTableByUidPlayerResponse
{
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
    public required bool IsSittingOut { get; init; }
}

public class GetTableByUidHandler(
    IRepository repository
) : IQueryHandler<GetTableByUidQuery, GetTableByUidResponse>
{
    public async Task<GetTableByUidResponse> HandleAsync(GetTableByUidQuery command)
    {
        var table = Table.FromEvents(
            uid: command.Uid,
            events: await repository.GetEventsAsync(command.Uid)
        );

        return new GetTableByUidResponse
        {
            Uid = table.Uid,
            Game = table.Game.ToString(),
            MaxSeat = table.MaxSeat,
            SmallBlind = table.SmallBlind,
            BigBlind = table.BigBlind,
            ChipCostAmount = table.ChipCost.Amount,
            ChipCostCurrency = table.ChipCost.Currency.ToString(),
            Players = table.Players.Select(SerializePlayer).ToList()
        };
    }

    private GetTableByUidPlayerResponse SerializePlayer(Player player)
    {
        return new GetTableByUidPlayerResponse
        {
            Nickname = player.Nickname,
            Seat = player.Seat,
            Stack = player.Stack,
            IsSittingOut = player.IsSittingOut
        };
    }
}

using Application.Repository;
using Application.Service.Hand;
using Domain.Entity;

namespace Application.Query;

public record GetTableByUidQuery : IQuery
{
    public required Guid Uid { get; init; }
}

public record GetTableByUidResponse : IQueryResponse
{
    public required Guid Uid { get; init; }
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required int SmallBlind { get; init; }
    public required int BigBlind { get; init; }
    public required decimal ChipCostAmount { get; init; }
    public required string ChipCostCurrency { get; init; }
    public required List<GetTableByUidResponsePlayer> Players { get; init; }
    public required GetTableByUidResponseHandState? HandState { get; init; }
}

public record GetTableByUidResponsePlayer
{
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
    public required bool IsSittingOut { get; init; }
}

public record GetTableByUidResponseHandState
{
    public required GetTableByUidResponseHandStateTable Table { get; init; }
    public required GetTableByUidResponseHandStatePot Pot { get; init; }
}

public record GetTableByUidResponseHandStateTable
{
    public required List<GetTableByUidResponseHandStatePlayer> Players { get; init; }
    public required string BoardCards { get; init; }
}

public record GetTableByUidResponseHandStatePlayer
{
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
    public required string HoleCards { get; init; }
    public required bool IsFolded { get; init; }
}

public record GetTableByUidResponseHandStatePot
{
    public required int Ante { get; init; }
    public required List<GetTableByUidResponseHandStateBet> CommittedBets { get; init; }
    public required List<GetTableByUidResponseHandStateBet> UncommittedBets { get; init; }
    public required List<GetTableByUidResponseHandStateAward> Awards { get; init; }
}

public record GetTableByUidResponseHandStateBet
{
    public required string Nickname { get; init; }
    public required int Amount { get; init; }
}

public record GetTableByUidResponseHandStateAward
{
    public required List<string> Nicknames { get; init; }
    public required int Amount { get; init; }
}

public class GetTableByUidHandler(
    IRepository repository,
    IHandService handService
) : IQueryHandler<GetTableByUidQuery, GetTableByUidResponse>
{
    public async Task<GetTableByUidResponse> HandleAsync(GetTableByUidQuery command)
    {
        var table = Table.FromEvents(
            uid: command.Uid,
            events: await repository.GetEventsAsync(command.Uid)
        );

        GetTableByUidResponseHandState? handState = null;
        if (table.IsHandInProgress())
        {
            var state = await handService.GetAsync(table.GetCurrentHandUid());
            handState = SerializeHandState(state);
        }

        return new GetTableByUidResponse
        {
            Uid = table.Uid,
            Game = table.Game.ToString(),
            MaxSeat = table.MaxSeat,
            SmallBlind = table.SmallBlind,
            BigBlind = table.BigBlind,
            ChipCostAmount = table.ChipCost.Amount,
            ChipCostCurrency = table.ChipCost.Currency.ToString(),
            Players = table.Players.Select(SerializePlayer).ToList(),
            HandState = handState
        };
    }

    private GetTableByUidResponsePlayer SerializePlayer(Player player)
    {
        return new GetTableByUidResponsePlayer
        {
            Nickname = player.Nickname,
            Seat = player.Seat,
            Stack = player.Stack,
            IsSittingOut = player.IsSittingOut
        };
    }

    private GetTableByUidResponseHandState SerializeHandState(HandState state)
    {
        return new GetTableByUidResponseHandState
        {
            Table = SerializeHandStateTable(state.Table),
            Pot = SerializeHandStatePot(state.Pot)
        };
    }

    private GetTableByUidResponseHandStateTable SerializeHandStateTable(HandStateTable table)
    {
        return new GetTableByUidResponseHandStateTable
        {
            Players = table.Players.Select(SerializeHandStatePlayer).ToList(),
            BoardCards = table.BoardCards
        };
    }

    private GetTableByUidResponseHandStatePlayer SerializeHandStatePlayer(HandStatePlayer player)
    {
        return new GetTableByUidResponseHandStatePlayer
        {
            Nickname = player.Nickname,
            Seat = player.Seat,
            Stack = player.Stack,
            HoleCards = player.HoleCards, // TODO: hide hole cards for villains
            IsFolded = player.IsFolded
        };
    }

    private GetTableByUidResponseHandStatePot SerializeHandStatePot(HandStatePot pot)
    {
        return new GetTableByUidResponseHandStatePot
        {
            Ante = pot.Ante,
            CommittedBets = pot.CommittedBets.Select(SerializeHandStateBet).ToList(),
            UncommittedBets = pot.UncommittedBets.Select(SerializeHandStateBet).ToList(),
            Awards = pot.Awards.Select(SerializeHandStateAward).ToList()
        };
    }

    private GetTableByUidResponseHandStateBet SerializeHandStateBet(HandStateBet bet)
    {
        return new GetTableByUidResponseHandStateBet
        {
            Nickname = bet.Nickname,
            Amount = bet.Amount
        };
    }

    private GetTableByUidResponseHandStateAward SerializeHandStateAward(HandStateAward award)
    {
        return new GetTableByUidResponseHandStateAward
        {
            Nicknames = award.Nicknames.Select(x => (string)x).ToList(),
            Amount = award.Amount
        };
    }
}

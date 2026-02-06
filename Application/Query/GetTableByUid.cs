using Application.Service.Hand;
using Application.Storage;
using Domain.ValueObject;

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
    public required decimal SmallBlind { get; init; }
    public required decimal BigBlind { get; init; }
    public required List<GetTableByUidResponsePlayer> Players { get; init; }
    public required GetTableByUidResponseHandState? HandState { get; init; }
}

public record GetTableByUidResponsePlayer
{
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required decimal Stack { get; init; }
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
    public required List<GetTableByUidResponseHandStateBet> CollectedBets { get; init; }
    public required List<GetTableByUidResponseHandStateBet> CurrentBets { get; init; }
    public required List<GetTableByUidResponseHandStateAward> Awards { get; init; }
}

public record GetTableByUidResponseHandStateBet
{
    public required string Nickname { get; init; }
    public required int Amount { get; init; }
}

public record GetTableByUidResponseHandStateAward
{
    public required List<string> Winners { get; init; }
    public required int Amount { get; init; }
}

public class GetTableByUidHandler(
    IStorage storage,
    IHandService handService
) : IQueryHandler<GetTableByUidQuery, GetTableByUidResponse>
{
    public async Task<GetTableByUidResponse> HandleAsync(GetTableByUidQuery command)
    {
        var view = await storage.GetDetailViewAsync(command.Uid);

        GetTableByUidResponseHandState? handState = null;
        if (view.CurrentHandUid is not null)
        {
            var state = await handService.GetAsync((HandUid)view.CurrentHandUid);
            handState = SerializeHandState(state);
        }

        return new GetTableByUidResponse
        {
            Uid = view.Uid,
            Game = view.Game.ToString(),
            MaxSeat = view.MaxSeat,
            SmallBlind = view.SmallBlind.Amount,
            BigBlind = view.BigBlind.Amount,
            Players = view.Players.Select(SerializePlayer).ToList(),
            HandState = handState
        };
    }

    private GetTableByUidResponsePlayer SerializePlayer(DetailViewPlayer player)
    {
        return new GetTableByUidResponsePlayer
        {
            Nickname = player.Nickname,
            Seat = player.Seat,
            Stack = player.Stack.Amount,
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
            CollectedBets = pot.CollectedBets.Select(SerializeHandStateBet).ToList(),
            CurrentBets = pot.CurrentBets.Select(SerializeHandStateBet).ToList(),
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
            Winners = award.Winners.Select(x => (string)x).ToList(),
            Amount = award.Amount
        };
    }
}

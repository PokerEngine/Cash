using Application.Service.Hand;
using Application.Storage;
using Domain.ValueObject;

namespace Application.Query;

public record GetTableDetailQuery : IQuery
{
    public required Guid Uid { get; init; }
}

public record GetTableDetailResponse : IQueryResponse
{
    public required Guid Uid { get; init; }
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required decimal SmallBlind { get; init; }
    public required decimal BigBlind { get; init; }
    public required List<GetTableDetailResponsePlayer> Players { get; init; }
    public required GetTableDetailResponseHandState? HandState { get; init; }
}

public record GetTableDetailResponsePlayer
{
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required decimal Stack { get; init; }
    public required bool IsSittingOut { get; init; }
}

public record GetTableDetailResponseHandState
{
    public required GetTableDetailResponseHandStateTable Table { get; init; }
    public required GetTableDetailResponseHandStatePot Pot { get; init; }
}

public record GetTableDetailResponseHandStateTable
{
    public required List<GetTableDetailResponseHandStatePlayer> Players { get; init; }
    public required string BoardCards { get; init; }
}

public record GetTableDetailResponseHandStatePlayer
{
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
    public required string HoleCards { get; init; }
    public required bool IsFolded { get; init; }
}

public record GetTableDetailResponseHandStatePot
{
    public required int Ante { get; init; }
    public required List<GetTableDetailResponseHandStateBet> CollectedBets { get; init; }
    public required List<GetTableDetailResponseHandStateBet> CurrentBets { get; init; }
    public required List<GetTableDetailResponseHandStateAward> Awards { get; init; }
}

public record GetTableDetailResponseHandStateBet
{
    public required string Nickname { get; init; }
    public required int Amount { get; init; }
}

public record GetTableDetailResponseHandStateAward
{
    public required List<string> Winners { get; init; }
    public required int Amount { get; init; }
}

public class GetTableDetailHandler(
    IStorage storage,
    IHandService handService
) : IQueryHandler<GetTableDetailQuery, GetTableDetailResponse>
{
    public async Task<GetTableDetailResponse> HandleAsync(GetTableDetailQuery query)
    {
        var view = await storage.GetDetailViewAsync(query.Uid);

        GetTableDetailResponseHandState? handState = null;
        if (view.CurrentHandUid is not null)
        {
            var state = await handService.GetAsync((HandUid)view.CurrentHandUid);
            handState = SerializeHandState(state);
        }

        return new GetTableDetailResponse
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

    private GetTableDetailResponsePlayer SerializePlayer(DetailViewPlayer player)
    {
        return new GetTableDetailResponsePlayer
        {
            Nickname = player.Nickname,
            Seat = player.Seat,
            Stack = player.Stack.Amount,
            IsSittingOut = player.IsSittingOut
        };
    }

    private GetTableDetailResponseHandState SerializeHandState(HandState state)
    {
        return new GetTableDetailResponseHandState
        {
            Table = SerializeHandStateTable(state.Table),
            Pot = SerializeHandStatePot(state.Pot)
        };
    }

    private GetTableDetailResponseHandStateTable SerializeHandStateTable(HandStateTable table)
    {
        return new GetTableDetailResponseHandStateTable
        {
            Players = table.Players.Select(SerializeHandStatePlayer).ToList(),
            BoardCards = table.BoardCards
        };
    }

    private GetTableDetailResponseHandStatePlayer SerializeHandStatePlayer(HandStatePlayer player)
    {
        return new GetTableDetailResponseHandStatePlayer
        {
            Nickname = player.Nickname,
            Seat = player.Seat,
            Stack = player.Stack,
            HoleCards = player.HoleCards, // TODO: hide hole cards for villains
            IsFolded = player.IsFolded
        };
    }

    private GetTableDetailResponseHandStatePot SerializeHandStatePot(HandStatePot pot)
    {
        return new GetTableDetailResponseHandStatePot
        {
            Ante = pot.Ante,
            CollectedBets = pot.CollectedBets.Select(SerializeHandStateBet).ToList(),
            CurrentBets = pot.CurrentBets.Select(SerializeHandStateBet).ToList(),
            Awards = pot.Awards.Select(SerializeHandStateAward).ToList()
        };
    }

    private GetTableDetailResponseHandStateBet SerializeHandStateBet(HandStateBet bet)
    {
        return new GetTableDetailResponseHandStateBet
        {
            Nickname = bet.Nickname,
            Amount = bet.Amount
        };
    }

    private GetTableDetailResponseHandStateAward SerializeHandStateAward(HandStateAward award)
    {
        return new GetTableDetailResponseHandStateAward
        {
            Winners = award.Winners.Select(x => (string)x).ToList(),
            Amount = award.Amount
        };
    }
}

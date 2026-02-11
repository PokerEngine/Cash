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
    public required GetTableDetailResponseRules Rules { get; init; }
    public required List<GetTableDetailResponsePlayer> Players { get; init; }
    public required GetTableDetailResponseCurrentHand? CurrentHand { get; init; }
}

public record GetTableDetailResponseRules
{
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required decimal SmallBlind { get; init; }
    public required decimal BigBlind { get; init; }
}

public record GetTableDetailResponsePlayer
{
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required decimal Stack { get; init; }
    public required bool IsSittingOut { get; init; }
}

public record GetTableDetailResponseCurrentHand
{
    public required GetTableDetailResponseCurrentHandTable Table { get; init; }
    public required GetTableDetailResponseCurrentHandPot Pot { get; init; }
}

public record GetTableDetailResponseCurrentHandTable
{
    public required List<GetTableDetailResponseCurrentHandPlayer> Players { get; init; }
    public required string BoardCards { get; init; }
}

public record GetTableDetailResponseCurrentHandPlayer
{
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required decimal Stack { get; init; }
    public required string HoleCards { get; init; }
    public required bool IsFolded { get; init; }
}

public record GetTableDetailResponseCurrentHandPot
{
    public required int Ante { get; init; }
    public required List<GetTableDetailResponseCurrentHandBet> CollectedBets { get; init; }
    public required List<GetTableDetailResponseCurrentHandBet> CurrentBets { get; init; }
    public required List<GetTableDetailResponseCurrentHandAward> Awards { get; init; }
}

public record GetTableDetailResponseCurrentHandBet
{
    public required string Nickname { get; init; }
    public required int Amount { get; init; }
}

public record GetTableDetailResponseCurrentHandAward
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

        GetTableDetailResponseCurrentHand? handState = null;
        if (view.CurrentHandUid is not null)
        {
            var state = await handService.GetAsync((HandUid)view.CurrentHandUid);
            handState = SerializeCurrentHand(state);
        }

        return new GetTableDetailResponse
        {
            Uid = view.Uid,
            Rules = SerializeRules(view.Rules),
            Players = view.Players.Select(SerializePlayer).ToList(),
            CurrentHand = handState
        };
    }

    private GetTableDetailResponseRules SerializeRules(DetailViewRules rules)
    {
        return new GetTableDetailResponseRules
        {
            Game = rules.Game,
            MaxSeat = rules.MaxSeat,
            SmallBlind = rules.SmallBlind,
            BigBlind = rules.BigBlind,
        };
    }

    private GetTableDetailResponsePlayer SerializePlayer(DetailViewPlayer player)
    {
        return new GetTableDetailResponsePlayer
        {
            Nickname = player.Nickname,
            Seat = player.Seat,
            Stack = player.Stack,
            IsSittingOut = player.IsSittingOut
        };
    }

    private GetTableDetailResponseCurrentHand SerializeCurrentHand(HandState hand)
    {
        return new GetTableDetailResponseCurrentHand
        {
            Table = SerializeCurrentHandTable(hand.Table),
            Pot = SerializeCurrentHandPot(hand.Pot)
        };
    }

    private GetTableDetailResponseCurrentHandTable SerializeCurrentHandTable(HandTable table)
    {
        return new GetTableDetailResponseCurrentHandTable
        {
            Players = table.Players.Select(SerializeCurrentHandPlayer).ToList(),
            BoardCards = table.BoardCards
        };
    }

    private GetTableDetailResponseCurrentHandPlayer SerializeCurrentHandPlayer(HandPlayer player)
    {
        return new GetTableDetailResponseCurrentHandPlayer
        {
            Nickname = player.Nickname,
            Seat = player.Seat,
            Stack = player.Stack,
            HoleCards = player.HoleCards, // TODO: hide hole cards for villains
            IsFolded = player.IsFolded
        };
    }

    private GetTableDetailResponseCurrentHandPot SerializeCurrentHandPot(HandPot pot)
    {
        return new GetTableDetailResponseCurrentHandPot
        {
            Ante = pot.Ante,
            CollectedBets = pot.CollectedBets.Select(SerializeCurrentHandBet).ToList(),
            CurrentBets = pot.CurrentBets.Select(SerializeCurrentHandBet).ToList(),
            Awards = pot.Awards.Select(SerializeCurrentHandAward).ToList()
        };
    }

    private GetTableDetailResponseCurrentHandBet SerializeCurrentHandBet(HandBet bet)
    {
        return new GetTableDetailResponseCurrentHandBet
        {
            Nickname = bet.Nickname,
            Amount = bet.Amount
        };
    }

    private GetTableDetailResponseCurrentHandAward SerializeCurrentHandAward(HandAward award)
    {
        return new GetTableDetailResponseCurrentHandAward
        {
            Winners = award.Winners.Select(x => (string)x).ToList(),
            Amount = award.Amount
        };
    }
}

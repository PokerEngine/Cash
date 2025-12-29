using Application.Service.Hand;
using Domain.ValueObject;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Service.Hand;

public class RemoteHandService(
    HttpClient httpClient,
    IOptions<RemoteHandServiceOptions> options
) : IHandService
{
    public async Task<HandState> GetAsync(HandUid handUid, CancellationToken cancellationToken = default)
    {
        var url = $"/api/hand/{handUid}";
        var response = await GetAsync<GetHandResponse>(url, cancellationToken);

        return new HandState
        {
            HandUid = response.HandUid,
            Players = response.State.Players.Select(p => new HandStatePlayer
            {
                Nickname = p.Nickname,
                Seat = p.Seat,
                Stack = p.Stack,
                HoleCards = ParseCards(p.HoleCards),
                IsFolded = false
            }).ToList(),
            BoardCards = ParseCards(response.State.BoardCards),
            Pot = new HandStatePot
            {
                DeadAmount = new Chips(0),
                Contributions = response.State.PreviousSidePot.Select(kv => new HandStateBet
                {
                    Nickname = kv.Key,
                    Amount = kv.Value
                }).ToList(),
            },
            Bets = response.State.CurrentSidePot.Select(kv => new HandStateBet
            {
                Nickname = kv.Key,
                Amount = kv.Value
            }).ToList(),
        };
    }

    public async Task<HandUid> CreateAsync(
        TableUid tableUid,
        Game game,
        Seat maxSeat,
        Chips smallBlind,
        Chips bigBlind,
        Seat? smallBlindSeat,
        Seat bigBlindSeat,
        Seat buttonSeat,
        IEnumerable<Participant> participants,
        CancellationToken cancellationToken = default
    )
    {
        var url = "/api/hand";
        var request = new CreateHandRequest
        {
            Game = game.ToString(),
            MaxSeat = maxSeat,
            SmallBlind = smallBlind,
            BigBlind = bigBlind,
            SmallBlindSeat = smallBlindSeat,
            BigBlindSeat = bigBlindSeat,
            ButtonSeat = buttonSeat,
            Participants = participants.Select(p => new CreateHandParticipantRequest
            {
                Nickname = p.Nickname,
                Seat = p.Seat,
                Stack = p.Stack
            }).ToList()
        };
        var response = await PostAsync<CreateHandRequest, CreateHandResponse>(url, request, cancellationToken);

        return response.HandUid;
    }

    public async Task StartAsync(
        HandUid handUid,
        CancellationToken cancellationToken = default
    )
    {
        var url = $"/api/hand/{handUid}/start";
        var request = new EmptyRequest();
        await PostAsync<EmptyRequest, EmptyResponse>(url, request, cancellationToken);
    }

    public async Task CommitDecisionAsync(
        HandUid handUid,
        Nickname nickname,
        DecisionType type,
        Chips amount,
        CancellationToken cancellationToken = default
    )
    {
        var url = $"/api/hand/{handUid}/commit-decision/{nickname}";
        var request = new CommitDecisionRequest
        {
            Type = type.ToString(),
            Amount = amount
        };
        await PostAsync<CommitDecisionRequest, EmptyResponse>(url, request, cancellationToken);
    }

    private async Task<TResponse> GetAsync<TResponse>(string url, CancellationToken cancellationToken)
    {
        var absoluteUrl = $"{options.Value.BaseUrl}{url}";
        var response = await httpClient.GetAsync(absoluteUrl, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(responseJson)!;
    }
    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        CancellationToken cancellationToken
    )
    {
        var absoluteUrl = $"{options.Value.BaseUrl}{url}";
        var requestJson = JsonSerializer.Serialize(request);
        var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");
        var response = await httpClient.PostAsync(absoluteUrl, requestContent, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(responseJson)!;
    }

    private List<HandStateCard> ParseCards(string cards)
    {
        var result = new List<HandStateCard>();
        for (int i = 0; i < cards.Length; i += 2)
        {
            result.Add(new HandStateCard(cards.Substring(i, 2)));
        }
        return result;
    }
}

public class RemoteHandServiceOptions
{
    public const string SectionName = "RemoteHand";

    public required string BaseUrl { get; init; }
}

internal sealed record GetHandResponse
{
    public required Guid HandUid { get; init; }
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required int SmallBlind { get; init; }
    public required int BigBlind { get; init; }
    public required int SmallBlindSeat { get; init; }
    public required int BigBlindSeat { get; init; }
    public required int ButtonSeat { get; init; }
    public required GetHandStateResponse State { get; init; }
}

internal sealed record GetHandStateResponse
{
    public required IReadOnlyList<GetHandStatePlayerResponse> Players { get; init; }
    public required string BoardCards { get; init; }
    public required IReadOnlyDictionary<string, int> PreviousSidePot { get; init; }
    public required IReadOnlyDictionary<string, int> CurrentSidePot { get; init; }
}

internal sealed record GetHandStatePlayerResponse
{
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
    public required string HoleCards { get; init; }
    public required bool IsFolded { get; init; }
}

internal sealed record CreateHandRequest
{
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required int SmallBlind { get; init; }
    public required int BigBlind { get; init; }
    public required int? SmallBlindSeat { get; init; }
    public required int BigBlindSeat { get; init; }
    public required int ButtonSeat { get; init; }
    public required IReadOnlyList<CreateHandParticipantRequest> Participants { get; init; }
}

internal sealed record CreateHandParticipantRequest
{
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
}

internal sealed record CreateHandResponse
{
    public required Guid HandUid { get; init; }
}

internal sealed record CommitDecisionRequest
{
    public required string Type { get; init; }
    public required int Amount { get; init; }
}

internal sealed record EmptyRequest;
internal sealed record EmptyResponse;

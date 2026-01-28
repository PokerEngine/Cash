using Application.Service.Hand;
using Domain.ValueObject;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Service.Hand;

public class RemoteHandService(
    HttpClient httpClient,
    IOptions<RemoteHandServiceOptions> options,
    ILogger<RemoteHandService> logger
) : IHandService
{
    private const string TableType = "Cash";
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<HandState> GetAsync(HandUid handUid, CancellationToken cancellationToken = default)
    {
        var url = $"/api/hand/{handUid}";
        var response = await GetAsync<GetHandResponse>(url, cancellationToken);

        return new HandState
        {
            Table = DeserializeTable(response.State.Table),
            Pot = DeserializePot(response.State.Pot)
        };
    }

    public async Task<HandUid> StartAsync(
        TableUid tableUid,
        Game game,
        Seat maxSeat,
        Chips smallBlind,
        Chips bigBlind,
        Seat? smallBlindSeat,
        Seat bigBlindSeat,
        Seat buttonSeat,
        List<HandParticipant> participants,
        CancellationToken cancellationToken = default
    )
    {
        var url = "/api/hand";
        var request = new StartHandRequest
        {
            TableUid = tableUid,
            TableType = TableType,
            Game = game.ToString(),
            MaxSeat = maxSeat,
            SmallBlind = smallBlind,
            BigBlind = bigBlind,
            SmallBlindSeat = smallBlindSeat,
            BigBlindSeat = bigBlindSeat,
            ButtonSeat = buttonSeat,
            Participants = participants.Select(SerializeParticipant).ToList()
        };
        var response = await PostAsync<StartHandRequest, StartHandResponse>(url, request, cancellationToken);

        return response.Uid;
    }

    public async Task SubmitPlayerActionAsync(
        HandUid handUid,
        Nickname nickname,
        PlayerActionType type,
        Chips amount,
        CancellationToken cancellationToken = default
    )
    {
        var url = $"/api/hand/{handUid}/submit-action/{nickname}";
        var request = new SubmitPlayerActionRequest
        {
            Type = type.ToString(),
            Amount = amount
        };
        await PostAsync<SubmitPlayerActionRequest, EmptyResponse>(url, request, cancellationToken);
    }

    private async Task<TResponse> GetAsync<TResponse>(string url, CancellationToken cancellationToken)
    {
        var absoluteUrl = $"{options.Value.BaseUrl}{url}";

        logger.LogInformation("Send GET request to {Url}", absoluteUrl);

        var response = await httpClient.GetAsync(absoluteUrl, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Request failed with status {StatusCode}. Response: {Body}", response.StatusCode, body);

            response.EnsureSuccessStatusCode();
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        return JsonSerializer.Deserialize<TResponse>(responseJson, JsonSerializerOptions)!;
    }
    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        CancellationToken cancellationToken
    )
    {
        var absoluteUrl = $"{options.Value.BaseUrl}{url}";
        var requestJson = JsonSerializer.Serialize(request, JsonSerializerOptions);
        var requestContent = new StringContent(requestJson, Encoding.UTF8, "application/json");

        logger.LogInformation("Send POST request to {Url} with body {Body}", absoluteUrl, requestJson);

        var response = await httpClient.PostAsync(absoluteUrl, requestContent, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Request failed with status {StatusCode}. Response: {Body}", response.StatusCode, body);

            response.EnsureSuccessStatusCode();
        }

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

        return JsonSerializer.Deserialize<TResponse>(responseJson, JsonSerializerOptions)!;
    }

    private StartHandRequestParticipant SerializeParticipant(HandParticipant participant)
    {
        return new StartHandRequestParticipant
        {
            Nickname = participant.Nickname,
            Seat = participant.Seat,
            Stack = participant.Stack
        };
    }

    private HandStateTable DeserializeTable(GetHandResponseStateTable table)
    {
        return new HandStateTable
        {
            Players = table.Players.Select(DeserializePlayer).ToList(),
            BoardCards = table.BoardCards
        };
    }

    private HandStatePlayer DeserializePlayer(GetHandResponseStatePlayer player)
    {
        return new HandStatePlayer
        {
            Nickname = player.Nickname,
            Seat = player.Seat,
            Stack = player.Stack,
            HoleCards = player.HoleCards,
            IsFolded = player.IsFolded
        };
    }

    private HandStatePot DeserializePot(GetHandResponseStatePot pot)
    {
        return new HandStatePot
        {
            Ante = pot.Ante,
            CollectedBets = pot.CollectedBets.Select(DeserializeBet).ToList(),
            CurrentBets = pot.CurrentBets.Select(DeserializeBet).ToList(),
            Awards = pot.Awards.Select(DeserializeAward).ToList()
        };
    }

    private HandStateBet DeserializeBet(GetHandResponseStateBet bet)
    {
        return new HandStateBet
        {
            Nickname = bet.Nickname,
            Amount = bet.Amount
        };
    }

    private HandStateAward DeserializeAward(GetHandResponseStateAward award)
    {
        return new HandStateAward
        {
            Winners = award.Winners.Select(n => new Nickname(n)).ToList(),
            Amount = award.Amount
        };
    }
}

public class RemoteHandServiceOptions
{
    public const string SectionName = "RemoteHand";

    public required string BaseUrl { get; init; }
}

internal sealed record GetHandResponse
{
    public required Guid Uid { get; init; }
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required int SmallBlind { get; init; }
    public required int BigBlind { get; init; }
    public required int SmallBlindSeat { get; init; }
    public required int BigBlindSeat { get; init; }
    public required int ButtonSeat { get; init; }
    public required GetHandResponseState State { get; init; }
}

internal sealed record GetHandResponseState
{
    public required GetHandResponseStateTable Table { get; init; }
    public required GetHandResponseStatePot Pot { get; init; }
}

internal sealed record GetHandResponseStateTable
{
    public required List<GetHandResponseStatePlayer> Players { get; init; }
    public required string BoardCards { get; init; }
}

internal sealed record GetHandResponseStatePlayer
{
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
    public required string HoleCards { get; init; }
    public required bool IsFolded { get; init; }
}

internal sealed record GetHandResponseStatePot
{
    public required int Ante { get; init; }
    public required List<GetHandResponseStateBet> CollectedBets { get; init; }
    public required List<GetHandResponseStateBet> CurrentBets { get; init; }
    public required List<GetHandResponseStateAward> Awards { get; init; }
}

internal sealed record GetHandResponseStateBet
{
    public required string Nickname { get; init; }
    public required int Amount { get; init; }
}

internal sealed record GetHandResponseStateAward
{
    public required List<string> Winners { get; init; }
    public required int Amount { get; init; }
}

internal sealed record StartHandRequest
{
    public required Guid TableUid { get; init; }
    public required string TableType { get; init; }
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required int SmallBlind { get; init; }
    public required int BigBlind { get; init; }
    public required int? SmallBlindSeat { get; init; }
    public required int BigBlindSeat { get; init; }
    public required int ButtonSeat { get; init; }
    public required List<StartHandRequestParticipant> Participants { get; init; }
}

internal sealed record StartHandRequestParticipant
{
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
}

internal sealed record StartHandResponse
{
    public required Guid Uid { get; init; }
}

internal sealed record SubmitPlayerActionRequest
{
    public required string Type { get; init; }
    public required int Amount { get; init; }
}

internal sealed record EmptyResponse;

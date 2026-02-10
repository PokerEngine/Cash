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
            Uid = response.Uid,
            TableUid = response.TableUid,
            Rules = DeserializeRules(response.Rules),
            Table = DeserializeTable(response.Table),
            Pot = DeserializePot(response.Pot)
        };
    }

    public async Task<HandUid> StartAsync(
        TableUid tableUid,
        HandRules rules,
        HandTable table,
        CancellationToken cancellationToken = default
    )
    {
        var url = "/api/hand";
        var request = new StartHandRequest
        {
            TableUid = tableUid,
            TableType = TableType,
            Rules = SerializeRules(rules),
            Table = SerializeTable(table)
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

    private HandServiceRules SerializeRules(HandRules rules)
    {
        return new HandServiceRules
        {
            Game = rules.Game.ToString(),
            MaxSeat = rules.MaxSeat,
            SmallBlind = rules.SmallBlind,
            BigBlind = rules.BigBlind
        };
    }

    private HandServiceTable SerializeTable(HandTable table)
    {
        return new HandServiceTable
        {
            Positions = SerializePositions(table.Positions),
            Players = table.Players.Select(SerializePlayer).ToList()
        };
    }

    private HandServicePositions SerializePositions(HandPositions positions)
    {
        return new HandServicePositions
        {
            SmallBlindSeat = positions.SmallBlindSeat,
            BigBlindSeat = positions.BigBlindSeat,
            ButtonSeat = positions.ButtonSeat
        };
    }

    private HandServicePlayer SerializePlayer(HandPlayer player)
    {
        return new HandServicePlayer
        {
            Nickname = player.Nickname,
            Seat = player.Seat,
            Stack = player.Stack
        };
    }

    private HandRules DeserializeRules(HandServiceRules rules)
    {
        return new HandRules
        {
            Game = (Game)Enum.Parse(typeof(Game), rules.Game),
            MaxSeat = rules.MaxSeat,
            SmallBlind = rules.SmallBlind,
            BigBlind = rules.BigBlind
        };
    }

    private HandTable DeserializeTable(HandServiceTable table)
    {
        return new HandTable
        {
            Positions = DeserializePositions(table.Positions),
            Players = table.Players.Select(DeserializePlayer).ToList(),
            BoardCards = table.BoardCards
        };
    }

    private HandPositions DeserializePositions(HandServicePositions positions)
    {
        return new HandPositions
        {
            SmallBlindSeat = positions.SmallBlindSeat,
            BigBlindSeat = positions.BigBlindSeat,
            ButtonSeat = positions.ButtonSeat
        };
    }

    private HandPlayer DeserializePlayer(HandServicePlayer player)
    {
        return new HandPlayer
        {
            Nickname = player.Nickname,
            Seat = player.Seat,
            Stack = player.Stack,
            HoleCards = player.HoleCards,
            IsFolded = player.IsFolded
        };
    }

    private HandPot DeserializePot(HandServicePot pot)
    {
        return new HandPot
        {
            Ante = pot.Ante,
            CollectedBets = pot.CollectedBets.Select(DeserializeBet).ToList(),
            CurrentBets = pot.CurrentBets.Select(DeserializeBet).ToList(),
            Awards = pot.Awards.Select(DeserializeAward).ToList()
        };
    }

    private HandBet DeserializeBet(HandServiceBet bet)
    {
        return new HandBet
        {
            Nickname = bet.Nickname,
            Amount = bet.Amount
        };
    }

    private HandAward DeserializeAward(HandServiceAward award)
    {
        return new HandAward
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
    public required Guid TableUid { get; init; }
    public required HandServiceRules Rules { get; init; }
    public required HandServiceTable Table { get; init; }
    public required HandServicePot Pot { get; init; }
}

internal sealed record StartHandRequest
{
    public required Guid TableUid { get; init; }
    public required string TableType { get; init; }
    public required HandServiceRules Rules { get; init; }
    public required HandServiceTable Table { get; init; }
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

internal sealed record HandServiceTable
{
    public required HandServicePositions Positions { get; init; }
    public required List<HandServicePlayer> Players { get; init; }
    public string BoardCards { get; init; } = "";
}

internal sealed record HandServicePot
{
    public required int Ante { get; init; }
    public required List<HandServiceBet> CollectedBets { get; init; }
    public required List<HandServiceBet> CurrentBets { get; init; }
    public required List<HandServiceAward> Awards { get; init; }
}

internal sealed record HandServiceRules
{
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required int SmallBlind { get; init; }
    public required int BigBlind { get; init; }
}

internal sealed record HandServicePositions
{
    public required int? SmallBlindSeat { get; init; }
    public required int BigBlindSeat { get; init; }
    public required int ButtonSeat { get; init; }
}

internal sealed record HandServicePlayer
{
    public required string Nickname { get; init; }
    public required int Seat { get; init; }
    public required int Stack { get; init; }
    public string HoleCards { get; init; } = "";
    public bool IsFolded { get; init; } = false;
}

internal sealed record HandServiceBet
{
    public required string Nickname { get; init; }
    public required int Amount { get; init; }
}

internal sealed record HandServiceAward
{
    public required List<string> Winners { get; init; }
    public required int Amount { get; init; }
}

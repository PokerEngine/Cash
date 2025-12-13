using Application.Service.Hand;
using Domain.ValueObject;
using Microsoft.Extensions.Options;

namespace Infrastructure.Service.Hand;

public class RemoteHandService(
    HttpClient httpClient,
    IOptions<RemoteHandServiceOptions> options
) : IHandService
{
    // TODO: implement remote calls

    public async Task<HandState> GetAsync(
        HandUid handUid,
        CancellationToken cancellationToken = default
    )
    {
        await Task.CompletedTask;

        return new HandState(
            HandUid: handUid,
            Participants: []
        );
    }

    public async Task<HandState> CreateAsync(
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
        await Task.CompletedTask;

        return new HandState(
            HandUid: new HandUid(Guid.NewGuid()),
            Participants: participants.ToList()
        );
    }

    public async Task<HandState> StartAsync(
        HandUid handUid,
        CancellationToken cancellationToken = default
    )
    {
        return await GetAsync(handUid, cancellationToken);
    }

    public async Task<HandState> CommitDecisionAsync(
        HandUid handUid,
        Nickname nickname,
        Decision decision,
        CancellationToken cancellationToken = default
    )
    {
        return await GetAsync(handUid, cancellationToken);
    }
}

public class RemoteHandServiceOptions
{
    public const string SectionName = "RemoteHandService";
    public required string BaseUrl { init; get; }
}

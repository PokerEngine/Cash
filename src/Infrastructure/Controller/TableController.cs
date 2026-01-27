using Application.Command;
using Application.Query;
using Application.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Controller;

[ApiController]
[Route("api/table")]
[Produces("application/json")]
public class TableController(
    ICommandDispatcher commandDispatcher,
    IQueryDispatcher queryDispatcher,
    IRepository repository,
    ILogger<TableController> logger
) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateTableResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTable([FromBody] CreateTableRequest request)
    {
        var command = new CreateTableCommand
        {
            Game = request.Game,
            MaxSeat = request.MaxSeat,
            SmallBlind = request.SmallBlind,
            BigBlind = request.BigBlind,
            ChipCostAmount = request.ChipCostAmount,
            ChipCostCurrency = request.ChipCostCurrency
        };
        var response = await commandDispatcher.DispatchAsync<CreateTableCommand, CreateTableResponse>(command);
        return CreatedAtAction(nameof(GetTableByUid), new { uid = response.Uid }, response);
    }

    [HttpPost("{uid:guid}/sit-down/{nickname}")]
    [ProducesResponseType(typeof(SitPlayerDownResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SitPlayerDown(Guid uid, string nickname, [FromBody] SitPlayerDownRequest request)
    {
        var command = new SitPlayerDownCommand
        {
            Uid = uid,
            Nickname = nickname,
            Seat = request.Seat,
            Stack = request.Stack
        };
        var response = await commandDispatcher.DispatchAsync<SitPlayerDownCommand, SitPlayerDownResponse>(command);
        return Ok(response);
    }

    [HttpPost("{uid:guid}/stand-up/{nickname}")]
    [ProducesResponseType(typeof(StandPlayerUpResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StandPlayerUp(Guid uid, string nickname)
    {
        var command = new StandPlayerUpCommand
        {
            Uid = uid,
            Nickname = nickname
        };
        var response = await commandDispatcher.DispatchAsync<StandPlayerUpCommand, StandPlayerUpResponse>(command);
        return Ok(response);
    }

    [HttpPost("{uid:guid}/submit-player-action/{nickname}")]
    [ProducesResponseType(typeof(SubmitPlayerActionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitPlayerAction(Guid uid, string nickname, [FromBody] SubmitPlayerActionRequest request)
    {
        var command = new SubmitPlayerActionCommand
        {
            Uid = uid,
            Nickname = nickname,
            Type = request.Type,
            Amount = request.Amount
        };
        var response = await commandDispatcher.DispatchAsync<SubmitPlayerActionCommand, SubmitPlayerActionResponse>(command);
        return Ok(response);
    }

    [HttpGet("{uid:guid}")]
    [ProducesResponseType(typeof(GetTableByUidResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTableByUid(Guid uid)
    {
        var events = await repository.GetEventsAsync(uid);
        foreach (var @event in events)
        {
            logger.LogInformation($"{@event}");
        }

        var query = new GetTableByUidQuery { Uid = uid };
        var response = await queryDispatcher.DispatchAsync<GetTableByUidQuery, GetTableByUidResponse>(query);
        return Ok(response);
    }
}

public record CreateTableRequest
{
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required int SmallBlind { get; init; }
    public required int BigBlind { get; init; }
    public required decimal ChipCostAmount { get; init; }
    public required string ChipCostCurrency { get; init; }
}

public record SitPlayerDownRequest
{
    public required int Seat { get; init; }
    public required int Stack { get; init; }
}

public record SubmitPlayerActionRequest
{
    public required string Type { get; init; }
    public required int Amount { get; init; }
}

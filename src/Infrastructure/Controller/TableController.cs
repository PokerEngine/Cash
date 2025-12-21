using Application.Command;
using Application.Query;
using Infrastructure.Command;
using Infrastructure.Query;
using Microsoft.AspNetCore.Mvc;

namespace Infrastructure.Controller;

[ApiController]
[Route("api/table")]
[Produces("application/json")]
public class TableController(
    ICommandDispatcher commandDispatcher,
    IQueryDispatcher queryDispatcher
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

    [HttpPut("{uid:guid}/sit-down/{nickname}")]
    [ProducesResponseType(typeof(SitDownPlayerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SitDownPlayer(Guid uid, string nickname, [FromBody] SitDownPlayerRequest request)
    {
        var command = new SitDownPlayerCommand
        {
            TableUid = uid,
            Nickname = nickname,
            Seat = request.Seat,
            Stack = request.Stack
        };
        var response = await commandDispatcher.DispatchAsync<SitDownPlayerCommand, SitDownPlayerResponse>(command);
        return Ok(response);
    }

    [HttpPut("{uid:guid}/stand-up/{nickname}")]
    [ProducesResponseType(typeof(StandUpPlayerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> StandUpPlayer(Guid uid, string nickname)
    {
        var command = new StandUpPlayerCommand
        {
            TableUid = uid,
            Nickname = nickname
        };
        var response = await commandDispatcher.DispatchAsync<StandUpPlayerCommand, StandUpPlayerResponse>(command);
        return Ok(response);
    }

    [HttpGet("{uid:guid}")]
    [ProducesResponseType(typeof(GetTableByUidResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTableByUid(Guid uid)
    {
        var query = new GetTableByUidQuery { Uid = uid };
        var response = await queryDispatcher.DispatchAsync<GetTableByUidQuery, GetTableByUidResponse>(query);
        return Ok(response);
    }
}

public record struct CreateTableRequest
{
    public required string Game { get; init; }
    public required int MaxSeat { get; init; }
    public required int SmallBlind { get; init; }
    public required int BigBlind { get; init; }
    public required decimal ChipCostAmount { get; init; }
    public required string ChipCostCurrency { get; init; }
}

public record struct SitDownPlayerRequest
{
    public required int Seat { get; init; }
    public required int Stack { get; init; }
}

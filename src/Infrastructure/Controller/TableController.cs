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
    CommandDispatcher commandDispatcher,
    QueryDispatcher queryDispatcher
) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(CreateTableResult), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTable([FromBody] CreateTableCommand request)
    {
        var result = await commandDispatcher.DispatchAsync<CreateTableCommand, CreateTableResult>(request);
        return CreatedAtAction(nameof(GetTableByUid), new { uid = result.TableUid }, result);
    }

    [HttpGet("{uid:guid}")]
    [ProducesResponseType(typeof(GetTableByUidResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetTableByUid(Guid uid)
    {
        var query = new GetTableByUidQuery(uid);
        var response = await queryDispatcher.DispatchAsync<GetTableByUidQuery, GetTableByUidResponse>(query);
        return Ok(response);
    }
}

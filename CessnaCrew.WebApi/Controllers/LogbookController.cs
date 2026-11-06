using CessnaCrew.WebApi.Data;
using CessnaCrew.WebApi.Dto;
using CessnaCrew.WebApi.Infrastructure;
using CessnaCrew.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;

namespace CessnaCrew.WebApi.Controllers;

/// <summary>
/// 5 Logbuch – wird beim Erstellen eines Törns automatisch angelegt.
/// Schreiben dürfen Eigner und die Crew des Törns, solange das Logbuch offen ist.
/// </summary>
[ApiController]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class LogbookController(CessnaCrewDbContext db) : ApiControllerBase(db)
{
    // GET /api/turns/{id}/logbook
    [HttpGet(StaticRoutes.Logbook.ForTurn)]
    [ProducesResponseType<LogbookDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LogbookDto>> GetLogbook([FromRoute] int id)
    {
        if (!CanReadTurn(id))
            return Forbid();

        var logbook = await Db.Logbooks.Include(l => l.Rows).SingleOrDefaultAsync(l => l.TurnId == id);
        if (logbook is null)
            return NotFound();

        return Ok(logbook.ToDto());
    }

    // PUT /api/turns/{id}/logbook
    [HttpPut(StaticRoutes.Logbook.ForTurn)]
    [ProducesResponseType<LogbookDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LogbookDto>> UpdateLogbook([FromRoute] int id, [FromBody] UpdateLogbookDto request)
    {
        if (!await CanWriteAsync(id))
            return Forbid();

        var logbook = await Db.Logbooks.Include(l => l.Rows).SingleOrDefaultAsync(l => l.TurnId == id);
        if (logbook is null)
            return NotFound();
        if (logbook.IsClosed)
            return LogbookClosed();

        if (request.EngineHoursStart is not null && request.EngineHoursEnd is not null
            && request.EngineHoursEnd < request.EngineHoursStart)
            return ValidationError(nameof(request.EngineHoursEnd), "Motorstunden am Ende müssen grösser als am Start sein.");

        logbook.IsHighSeaMode = request.IsHighSeaMode;
        logbook.EngineHoursStart = request.EngineHoursStart;
        logbook.EngineHoursEnd = request.EngineHoursEnd;

        await Db.SaveChangesAsync();
        return Ok(logbook.ToDto());
    }

    // POST /api/turns/{id}/logbook/rows
    [HttpPost(StaticRoutes.Logbook.Rows)]
    [ProducesResponseType<LogbookRowDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LogbookRowDto>> AddRow([FromRoute] int id, [FromBody] SaveLogbookRowDto request)
    {
        if (!await CanWriteAsync(id))
            return Forbid();

        var logbook = await Db.Logbooks.SingleOrDefaultAsync(l => l.TurnId == id);
        if (logbook is null)
            return NotFound();
        if (logbook.IsClosed)
            return LogbookClosed();

        var row = new LogbookRow
        {
            LogbookId = logbook.Id,
            CreatedByUserId = CurrentUserId         // null bei Token-Gästen
        };
        Apply(row, request);

        Db.LogbookRows.Add(row);
        await Db.SaveChangesAsync();
        return Created($"/{StaticRoutes.Turns.Base}/{id}/logbook/rows/{row.Id}", row.ToDto());
    }

    // PUT /api/turns/{id}/logbook/rows/{rowId}
    [HttpPut(StaticRoutes.Logbook.RowById)]
    [ProducesResponseType<LogbookRowDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LogbookRowDto>> UpdateRow(
        [FromRoute] int id, [FromRoute] int rowId, [FromBody] SaveLogbookRowDto request)
    {
        if (!await CanWriteAsync(id))
            return Forbid();

        var row = await FindRowAsync(id, rowId);
        if (row is null)
            return NotFound();
        if (row.Logbook.IsClosed)
            return LogbookClosed();

        Apply(row, request);
        await Db.SaveChangesAsync();
        return Ok(row.ToDto());
    }

    // DELETE /api/turns/{id}/logbook/rows/{rowId}
    [HttpDelete(StaticRoutes.Logbook.RowById)]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteRow([FromRoute] int id, [FromRoute] int rowId)
    {
        var row = await FindRowAsync(id, rowId);
        if (row is null)
            return NotFound();
        if (row.Logbook.Turn.OwnerId != CurrentUserId)
            return Forbid();                               // nur der Ersteller des Törns
        if (row.Logbook.IsClosed)
            return LogbookClosed();

        Db.LogbookRows.Remove(row);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Schreibrecht: jeder Eigner oder die Crew dieses Törns.</summary>
    private async Task<bool> CanWriteAsync(int turnId) =>
        User.IsOwner() || await IsCrewOfTurnAsync(turnId);

    private Task<LogbookRow?> FindRowAsync(int turnId, int rowId) => Db.LogbookRows
        .Include(r => r.Logbook).ThenInclude(l => l.Turn)
        .SingleOrDefaultAsync(r => r.Id == rowId && r.Logbook.TurnId == turnId);

    private static void Apply(LogbookRow row, SaveLogbookRowDto request)
    {
        row.LoggedAt = request.LoggedAt!.Value;
        row.Latitude = request.Latitude;
        row.Longitude = request.Longitude;
        row.WindForceBft = request.WindForceBft;
        row.WindDirection = request.WindDirection?.Trim();
        row.AirPressureHpa = request.AirPressureHpa;
        row.SailConfiguration = request.SailConfiguration?.Trim();
        row.Remarks = request.Remarks?.Trim();
    }

    private ObjectResult LogbookClosed() =>
        ConflictProblem("Das Logbuch ist geschlossen, der Törn wurde bereits abgeschlossen.");
}

using CessnaCrew.WebApi.Data;
using CessnaCrew.WebApi.Dto;
using CessnaCrew.WebApi.Infrastructure;
using CessnaCrew.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

namespace CessnaCrew.WebApi.Controllers;

/// <summary>
/// 2 Belegungskalender & Törnplanung.
/// </summary>
[ApiController]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class TurnsController(CessnaCrewDbContext db) : ApiControllerBase(db)
{
    // GET /api/turns?from=2026-07-01&to=2026-08-01
    [HttpGet(StaticRoutes.Turns.GetAll)]
    [Authorize(Roles = Roles.OwnerOrRegularGuest)]
    [ProducesResponseType<IEnumerable<TurnDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IEnumerable<TurnDto>>> GetTurns(
        [FromQuery, Required] DateTime? from,
        [FromQuery, Required] DateTime? to)
    {
        if (from >= to)
            return ValidationError(nameof(to), "'to' muss nach 'from' liegen.");

        // Alle Törns, die den Zeitraum zumindest teilweise überschneiden
        var turns = await Db.Turns
            .Include(t => t.Owner)
            .Include(t => t.Logbook)
            .Where(t => t.StartDateTime < to && t.EndDateTime > from)
            .OrderBy(t => t.StartDateTime)
            .ToListAsync();

        return Ok(turns.Select(t => t.ToDto()));
    }

    // GET /api/turns/history?year=2026
    [HttpGet(StaticRoutes.Turns.History)]
    [Authorize(Roles = Roles.OwnerOrRegularGuest)]
    [ProducesResponseType<IEnumerable<TurnDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<TurnDto>>> GetHistory([FromQuery] int? year)
    {
        // Vergangen = Ende liegt in der Vergangenheit oder Törn wurde offiziell abgeschlossen
        var now = DateTime.Now;
        var query = Db.Turns
            .Include(t => t.Owner)
            .Include(t => t.Logbook)
            .Where(t => t.EndDateTime < now || (t.Logbook != null && t.Logbook.IsClosed));

        if (year is not null)
            query = query.Where(t => t.StartDateTime.Year == year);

        var turns = await query.OrderByDescending(t => t.StartDateTime).ToListAsync();
        return Ok(turns.Select(t => t.ToDto()));
    }

    // GET /api/turns/{id}
    [HttpGet(StaticRoutes.Turns.ById)]
    [ProducesResponseType<TurnDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TurnDetailDto>> GetTurn([FromRoute] int id)
    {
        if (!CanReadTurn(id))
            return Forbid();

        var turn = await Db.Turns
            .Include(t => t.Owner)
            .Include(t => t.Logbook)
            .Include(t => t.CrewMembers)
            .SingleOrDefaultAsync(t => t.Id == id);

        if (turn is null)
            return NotFound();

        return Ok(turn.ToDetailDto());
    }

    // POST /api/turns
    [HttpPost(StaticRoutes.Turns.GetAll)]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType<TurnDetailDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TurnDetailDto>> CreateTurn([FromBody] SaveTurnDto request)
    {
        var start = request.StartDateTime!.Value;
        var end = request.EndDateTime!.Value;

        if (start >= end)
            return ValidationError(nameof(request.EndDateTime), "Das Ende muss nach dem Start liegen.");

        if (await HasCollisionAsync(start, end))
            return ConflictProblem("Der Zeitraum überschneidet sich mit einem bestehenden Törn.");

        var turn = new Turn
        {
            Title = request.Title.Trim(),
            StartDateTime = start,
            EndDateTime = end,
            Type = request.Type!.Value,
            OwnerId = CurrentUserId!.Value,
            Logbook = new Logbook()        // Logbuch wird automatisch mit angelegt
        };

        Db.Turns.Add(turn);
        await Db.SaveChangesAsync();
        await Db.Entry(turn).Reference(t => t.Owner).LoadAsync();

        return Created($"/{StaticRoutes.Turns.Base}/{turn.Id}", turn.ToDetailDto());
    }

    // PUT /api/turns/{id}
    [HttpPut(StaticRoutes.Turns.ById)]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType<TurnDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TurnDetailDto>> UpdateTurn([FromRoute] int id, [FromBody] SaveTurnDto request)
    {
        var turn = await Db.Turns
            .Include(t => t.Owner)
            .Include(t => t.Logbook)
            .Include(t => t.CrewMembers)
            .SingleOrDefaultAsync(t => t.Id == id);

        if (turn is null)
            return NotFound();
        if (turn.OwnerId != CurrentUserId)
            return Forbid();                               // nur der Ersteller

        var start = request.StartDateTime!.Value;
        var end = request.EndDateTime!.Value;

        if (start >= end)
            return ValidationError(nameof(request.EndDateTime), "Das Ende muss nach dem Start liegen.");

        if (await HasCollisionAsync(start, end, excludeTurnId: id))
            return ConflictProblem("Der Zeitraum überschneidet sich mit einem bestehenden Törn.");

        turn.Title = request.Title.Trim();
        turn.StartDateTime = start;
        turn.EndDateTime = end;
        turn.Type = request.Type!.Value;

        await Db.SaveChangesAsync();
        return Ok(turn.ToDetailDto());
    }

    // DELETE /api/turns/{id}
    [HttpDelete(StaticRoutes.Turns.ById)]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTurn([FromRoute] int id)
    {
        var turn = await Db.Turns.FindAsync(id);
        if (turn is null)
            return NotFound();
        if (turn.OwnerId != CurrentUserId)
            return Forbid();

        // Tausch-Anfragen, in denen dieser Törn angeboten wurde, löscht die DB nicht automatisch (NoAction)
        var offeredIn = await Db.SwapRequests.Where(s => s.OfferedTurnId == id).ToListAsync();
        Db.SwapRequests.RemoveRange(offeredIn);

        // Crew, Logbuch inkl. Einträge und Tausch-Anfragen auf diesen Slot werden per Cascade gelöscht
        Db.Turns.Remove(turn);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/turns/{id}/complete
    [HttpPost(StaticRoutes.Turns.Complete)]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType<TurnDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TurnDetailDto>> CompleteTurn(
        [FromRoute] int id,
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] CompleteTurnDto? request)
    {
        var turn = await Db.Turns
            .Include(t => t.Owner)
            .Include(t => t.Logbook)
            .Include(t => t.CrewMembers)
            .SingleOrDefaultAsync(t => t.Id == id);

        if (turn is null)
            return NotFound();
        if (turn.OwnerId != CurrentUserId)
            return Forbid();
        if (turn.IsCompleted())
            return ConflictProblem("Der Törn ist bereits abgeschlossen.");

        // Einladungs-Link deaktivieren → Gast-Tokens werden ab sofort abgelehnt (siehe Program.cs)
        turn.IsTokenActive = false;

        turn.Logbook ??= new Logbook();
        turn.Logbook.IsClosed = true;
        if (request?.EngineHoursEnd is not null)
            turn.Logbook.EngineHoursEnd = request.EngineHoursEnd;

        await Db.SaveChangesAsync();
        return Ok(turn.ToDetailDto());
    }

    /// <summary>
    /// Kollisionsschutz: zwei Zeiträume überschneiden sich, wenn A.Start &lt; B.Ende und B.Start &lt; A.Ende.
    /// Gilt auch für Wartungsblöcke.
    /// </summary>
    private Task<bool> HasCollisionAsync(DateTime start, DateTime end, int excludeTurnId = 0) =>
        Db.Turns.AnyAsync(t => t.Id != excludeTurnId && t.StartDateTime < end && start < t.EndDateTime);
}

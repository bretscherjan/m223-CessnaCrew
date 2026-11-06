using System.Buffers.Text;
using System.Security.Cryptography;
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
/// 4 Crew & Einladungen – Crew eines Törns verwalten und Einladungs-Links erzeugen.
/// </summary>
[ApiController]
[Authorize]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class CrewController(CessnaCrewDbContext db) : ApiControllerBase(db)
{
    // GET /api/turns/{id}/crew
    [HttpGet(StaticRoutes.Crew.GetAll)]
    [ProducesResponseType<IEnumerable<CrewMemberDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<CrewMemberDto>>> GetCrew([FromRoute] int id)
    {
        if (!CanReadTurn(id))
            return Forbid();
        if (!await Db.Turns.AnyAsync(t => t.Id == id))
            return NotFound();

        var crew = await Db.CrewMembers.Where(c => c.TurnId == id).OrderBy(c => c.Id).ToListAsync();
        return Ok(crew.Select(c => c.ToDto()));
    }

    // POST /api/turns/{id}/crew
    [HttpPost(StaticRoutes.Crew.GetAll)]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType<CrewMemberDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CrewMemberDto>> AddCrewMember([FromRoute] int id, [FromBody] AddCrewMemberDto request)
    {
        var turn = await Db.Turns.FindAsync(id);
        if (turn is null)
            return NotFound();
        if (turn.OwnerId != CurrentUserId)
            return Forbid();

        var user = await Db.Users.FindAsync(request.UserId);
        if (user is null)
            return ValidationError(nameof(request.UserId), "Diesen Nutzer gibt es nicht.");

        if (await Db.CrewMembers.AnyAsync(c => c.TurnId == id && c.UserId == user.Id))
            return ConflictProblem("Der Nutzer ist bereits in der Crew.");

        var member = new CrewMember
        {
            TurnId = id,
            UserId = user.Id,
            DisplayName = user.Username,
            RoleOnBoard = request.RoleOnBoard!.Value
        };

        Db.CrewMembers.Add(member);
        await Db.SaveChangesAsync();
        return Created($"/{StaticRoutes.Turns.Base}/{id}/crew/{member.Id}", member.ToDto());
    }

    // PUT /api/turns/{id}/crew/{memberId}
    [HttpPut(StaticRoutes.Crew.ById)]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType<CrewMemberDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CrewMemberDto>> UpdateCrewMember(
        [FromRoute] int id, [FromRoute] int memberId, [FromBody] UpdateCrewMemberDto request)
    {
        var member = await Db.CrewMembers.Include(c => c.Turn)
            .SingleOrDefaultAsync(c => c.Id == memberId && c.TurnId == id);
        if (member is null)
            return NotFound();
        if (member.Turn.OwnerId != CurrentUserId)
            return Forbid();

        member.RoleOnBoard = request.RoleOnBoard!.Value;
        await Db.SaveChangesAsync();
        return Ok(member.ToDto());
    }

    // DELETE /api/turns/{id}/crew/{memberId}
    [HttpDelete(StaticRoutes.Crew.ById)]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveCrewMember([FromRoute] int id, [FromRoute] int memberId)
    {
        var member = await Db.CrewMembers.Include(c => c.Turn)
            .SingleOrDefaultAsync(c => c.Id == memberId && c.TurnId == id);
        if (member is null)
            return NotFound();
        if (member.Turn.OwnerId != CurrentUserId)
            return Forbid();

        // Ein entfernter Token-Gast verliert damit auch seinen Zugriff (Prüfung in Program.cs)
        Db.CrewMembers.Remove(member);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    // POST /api/turns/{id}/invite-token
    [HttpPost(StaticRoutes.Crew.InviteToken)]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType<InviteTokenDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<InviteTokenDto>> CreateInviteToken([FromRoute] int id)
    {
        var turn = await Db.Turns.Include(t => t.Logbook).SingleOrDefaultAsync(t => t.Id == id);
        if (turn is null)
            return NotFound();
        if (turn.OwnerId != CurrentUserId)
            return Forbid();
        if (turn.IsCompleted())
            return ConflictProblem("Für einen abgeschlossenen Törn können keine Einladungen erstellt werden.");

        // Neuer, zufälliger Token. Ein alter Link wird damit ungültig.
        turn.InviteToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(24));
        turn.IsTokenActive = true;

        await Db.SaveChangesAsync();
        return Ok(new InviteTokenDto(turn.InviteToken, turn.IsTokenActive));
    }

    // DELETE /api/turns/{id}/invite-token
    [HttpDelete(StaticRoutes.Crew.InviteToken)]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeInviteToken([FromRoute] int id)
    {
        var turn = await Db.Turns.FindAsync(id);
        if (turn is null)
            return NotFound();
        if (turn.OwnerId != CurrentUserId)
            return Forbid();

        // Token bleibt gespeichert, damit ein Aufruf des alten Links 410 Gone statt 404 liefert
        turn.IsTokenActive = false;
        await Db.SaveChangesAsync();
        return NoContent();
    }
}

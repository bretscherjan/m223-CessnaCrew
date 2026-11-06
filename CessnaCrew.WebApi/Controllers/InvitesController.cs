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
/// 4 Crew & Einladungen – öffentliche Endpunkte für den Einladungs-Link.
/// </summary>
[ApiController]
[AllowAnonymous]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class InvitesController(CessnaCrewDbContext db, TokenService tokenService) : ApiControllerBase(db)
{
    // GET /api/invites/{token}
    [HttpGet(StaticRoutes.Invites.ByToken)]
    [ProducesResponseType<InviteDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status410Gone)]
    public async Task<ActionResult<InviteDto>> GetInvite([FromRoute] string token)
    {
        var turn = await FindTurnByTokenAsync(token);
        if (turn is null)
            return NotFound();
        if (!IsInviteValid(turn))
            return InviteExpired();

        return Ok(new InviteDto(turn.Id, turn.Title, turn.StartDateTime, turn.EndDateTime, turn.Owner.Username));
    }

    // POST /api/invites/{token}/join
    [HttpPost(StaticRoutes.Invites.Join)]
    [ProducesResponseType<GuestTokenDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status410Gone)]
    public async Task<ActionResult<GuestTokenDto>> Join([FromRoute] string token, [FromBody] JoinInviteDto request)
    {
        var turn = await FindTurnByTokenAsync(token);
        if (turn is null)
            return NotFound();
        if (!IsInviteValid(turn))
            return InviteExpired();

        var member = new CrewMember
        {
            TurnId = turn.Id,
            UserId = null,                         // anonymer Gast
            DisplayName = request.DisplayName.Trim(),
            RoleOnBoard = RoleOnBoard.Guest
        };

        Db.CrewMembers.Add(member);
        await Db.SaveChangesAsync();

        var (guestToken, expiresAt) = tokenService.CreateGuestToken(member);
        return Created($"/{StaticRoutes.Turns.Base}/{turn.Id}/crew/{member.Id}",
            new GuestTokenDto(guestToken, expiresAt, member.ToDto(), turn.Id));
    }

    private Task<Turn?> FindTurnByTokenAsync(string token) => Db.Turns
        .Include(t => t.Owner)
        .Include(t => t.Logbook)
        .SingleOrDefaultAsync(t => t.InviteToken == token);

    private static bool IsInviteValid(Turn turn) => turn.IsTokenActive && !turn.IsCompleted();

    private ObjectResult InviteExpired() =>
        Problem(detail: "Der Einladungs-Link ist abgelaufen.", statusCode: StatusCodes.Status410Gone);
}

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
/// 3 Slot-Tausch – nur für Eigner.
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Owner)]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class SwapRequestsController(CessnaCrewDbContext db) : ApiControllerBase(db)
{
    // POST /api/turns/{id}/swap-requests
    [HttpPost(StaticRoutes.SwapRequests.ForTurn)]
    [ProducesResponseType<SwapRequestDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SwapRequestDto>> CreateSwapRequest([FromRoute] int id, [FromBody] CreateSwapRequestDto request)
    {
        var userId = CurrentUserId!.Value;

        var requestedTurn = await Db.Turns.FindAsync(id);
        if (requestedTurn is null)
            return NotFound();
        if (requestedTurn.OwnerId == userId)
            return ValidationError("id", "Den eigenen Törn kann man nicht anfragen.");

        if (request.OfferedTurnId is int offeredId)
        {
            var offeredTurn = await Db.Turns.FindAsync(offeredId);
            if (offeredTurn is null || offeredTurn.OwnerId != userId)
                return ValidationError(nameof(request.OfferedTurnId), "Es kann nur ein eigener Törn angeboten werden.");
        }

        var alreadyOpen = await Db.SwapRequests.AnyAsync(s =>
            s.RequestedTurnId == id && s.RequesterId == userId && s.Status == SwapRequestStatus.Open);
        if (alreadyOpen)
            return ConflictProblem("Für diesen Törn besteht bereits eine offene Anfrage.");

        var swapRequest = new SwapRequest
        {
            RequestedTurnId = id,
            OfferedTurnId = request.OfferedTurnId,
            RequesterId = userId,
            Message = request.Message?.Trim() ?? string.Empty
        };

        Db.SwapRequests.Add(swapRequest);
        await Db.SaveChangesAsync();

        var created = await LoadAsync(swapRequest.Id);
        return Created($"/{StaticRoutes.SwapRequests.Base}/{swapRequest.Id}", created!.ToDto(userId));
    }

    // GET /api/swap-requests?status=Open
    [HttpGet(StaticRoutes.SwapRequests.GetAll)]
    [ProducesResponseType<IEnumerable<SwapRequestDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SwapRequestDto>>> GetSwapRequests([FromQuery] SwapRequestStatus? status)
    {
        var userId = CurrentUserId!.Value;

        // Eingehend: Anfragen auf meine Törns. Ausgehend: von mir gestellte Anfragen.
        var query = IncludeAll().Where(s => s.RequestedTurn.OwnerId == userId || s.RequesterId == userId);
        if (status is not null)
            query = query.Where(s => s.Status == status);

        var requests = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        return Ok(requests.Select(s => s.ToDto(userId)));
    }

    // PUT /api/swap-requests/{id}
    [HttpPut(StaticRoutes.SwapRequests.ById)]
    [ProducesResponseType<SwapRequestDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SwapRequestDto>> AnswerSwapRequest([FromRoute] int id, [FromBody] AnswerSwapRequestDto request)
    {
        var userId = CurrentUserId!.Value;

        var swapRequest = await LoadAsync(id);
        if (swapRequest is null)
            return NotFound();
        if (swapRequest.RequestedTurn.OwnerId != userId)
            return Forbid();                               // nur der angefragte Eigner antwortet
        if (swapRequest.Status != SwapRequestStatus.Open)
            return ConflictProblem("Die Anfrage wurde bereits beantwortet.");
        if (request.Status is not (SwapRequestStatus.Accepted or SwapRequestStatus.Rejected))
            return ValidationError(nameof(request.Status), "Status muss 'Accepted' oder 'Rejected' sein.");

        swapRequest.Status = request.Status.Value;

        if (swapRequest.Status == SwapRequestStatus.Accepted)
        {
            // Slots tauschen = Eigner tauschen. Die Zeiträume bleiben gleich, daher keine neue Kollision.
            swapRequest.RequestedTurn.OwnerId = swapRequest.RequesterId;
            if (swapRequest.OfferedTurn is not null)
                swapRequest.OfferedTurn.OwnerId = userId;
        }

        await Db.SaveChangesAsync();

        var updated = await LoadAsync(id);                 // Owner-Namen neu laden
        return Ok(updated!.ToDto(userId));
    }

    // DELETE /api/swap-requests/{id}
    [HttpDelete(StaticRoutes.SwapRequests.ById)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteSwapRequest([FromRoute] int id)
    {
        var swapRequest = await Db.SwapRequests.FindAsync(id);
        if (swapRequest is null)
            return NotFound();
        if (swapRequest.RequesterId != CurrentUserId)
            return Forbid();                               // nur der Anfragende zieht zurück
        if (swapRequest.Status != SwapRequestStatus.Open)
            return ConflictProblem("Nur offene Anfragen können zurückgezogen werden.");

        Db.SwapRequests.Remove(swapRequest);
        await Db.SaveChangesAsync();
        return NoContent();
    }

    private IQueryable<SwapRequest> IncludeAll() => Db.SwapRequests
        .Include(s => s.Requester)
        .Include(s => s.RequestedTurn).ThenInclude(t => t.Owner)
        .Include(s => s.RequestedTurn).ThenInclude(t => t.Logbook)
        .Include(s => s.OfferedTurn).ThenInclude(t => t!.Owner)
        .Include(s => s.OfferedTurn).ThenInclude(t => t!.Logbook);

    private Task<SwapRequest?> LoadAsync(int id) => IncludeAll().SingleOrDefaultAsync(s => s.Id == id);
}

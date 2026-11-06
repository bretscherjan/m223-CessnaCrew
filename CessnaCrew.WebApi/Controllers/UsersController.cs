using CessnaCrew.WebApi.Data;
using CessnaCrew.WebApi.Dto;
using CessnaCrew.WebApi.Infrastructure;
using CessnaCrew.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;

namespace CessnaCrew.WebApi.Controllers;

/// <summary>
/// 1 Benutzerverwaltung – Profile abrufen, bearbeiten, löschen.
/// </summary>
[ApiController]
[Authorize(Roles = Roles.OwnerOrRegularGuest)]   // Token-Gäste haben kein Profil
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class UsersController(CessnaCrewDbContext db, IPasswordHasher<User> passwordHasher) : ApiControllerBase(db)
{
    // GET /api/users?role=Owner
    [HttpGet(StaticRoutes.Users.GetAll)]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType<IEnumerable<UserDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetUsers([FromQuery] UserRole? role)
    {
        var query = Db.Users.AsQueryable();
        if (role is not null)
            query = query.Where(u => u.Role == role);

        var users = await query.OrderBy(u => u.Username).ToListAsync();
        return Ok(users.Select(u => u.ToDto()));
    }

    // GET /api/users/me
    [HttpGet(StaticRoutes.Users.Me)]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var user = await Db.Users.FindAsync(CurrentUserId);
        if (user is null)
            return NotFound();

        return Ok(user.ToDto());
    }

    // GET /api/users/{id}
    [HttpGet(StaticRoutes.Users.ById)]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUserById([FromRoute] int id)
    {
        if (!User.IsOwner() && id != CurrentUserId)
            return Forbid();

        var user = await Db.Users.FindAsync(id);
        if (user is null)
            return NotFound();

        return Ok(user.ToDto());
    }

    // PUT /api/users/{id}
    [HttpPut(StaticRoutes.Users.ById)]
    [ProducesResponseType<UserDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> UpdateUser([FromRoute] int id, [FromBody] UpdateUserDto request)
    {
        if (id != CurrentUserId)
            return Forbid();                               // nur der eigene Account

        var user = await Db.Users.FindAsync(id);
        if (user is null)
            return NotFound();

        var email = request.Email.Trim().ToLowerInvariant();
        if (await Db.Users.AnyAsync(u => u.Email == email && u.Id != id))
            return ConflictProblem("Diese E-Mail ist bereits registriert.");

        user.Username = request.Username.Trim();
        user.Email = email;
        if (!string.IsNullOrEmpty(request.Password))
            user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        await Db.SaveChangesAsync();
        return Ok(user.ToDto());
    }

    // DELETE /api/users/{id}
    [HttpDelete(StaticRoutes.Users.ById)]
    [Authorize(Roles = Roles.Owner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteUser([FromRoute] int id)
    {
        var user = await Db.Users.FindAsync(id);
        if (user is null)
            return NotFound();

        // Diese Beziehungen sind in der DB mit Restrict geschützt (siehe CessnaCrewDbContext)
        var hasDependencies =
            await Db.Turns.AnyAsync(t => t.OwnerId == id)
            || await Db.Invoices.AnyAsync(i => i.PayerId == id)
            || await Db.InvoiceSplits.AnyAsync(s => s.UserId == id)
            || await Db.SwapRequests.AnyAsync(s => s.RequesterId == id);

        if (hasDependencies)
            return ConflictProblem("Der Nutzer hat noch Törns, Rechnungen oder Tausch-Anfragen und kann nicht gelöscht werden.");

        Db.Users.Remove(user);
        await Db.SaveChangesAsync();
        return NoContent();
    }
}

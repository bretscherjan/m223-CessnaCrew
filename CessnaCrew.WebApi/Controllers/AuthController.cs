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
/// 1 Benutzerverwaltung – Registrierung und Anmeldung (öffentlich).
/// </summary>
[ApiController]
[AllowAnonymous]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class AuthController(
    CessnaCrewDbContext db,
    IPasswordHasher<User> passwordHasher,
    TokenService tokenService) : ApiControllerBase(db)
{
    // POST /api/auth/register
    [HttpPost(StaticRoutes.Auth.Register)]
    [ProducesResponseType<UserDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await Db.Users.AnyAsync(u => u.Email == email))
            return ConflictProblem("Diese E-Mail ist bereits registriert.");

        var user = new User
        {
            Username = request.Username.Trim(),
            Email = email,
            Role = request.Role
        };
        user.PasswordHash = passwordHasher.HashPassword(user, request.Password); // nie im Klartext speichern

        Db.Users.Add(user);
        await Db.SaveChangesAsync();

        return Created($"/{StaticRoutes.Users.Base}/{user.Id}", user.ToDto());
    }

    // POST /api/auth/login
    [HttpPost(StaticRoutes.Auth.Login)]
    [ProducesResponseType<LoginResponseDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await Db.Users.SingleOrDefaultAsync(u => u.Email == email);

        // Gleiche Meldung für "E-Mail unbekannt" und "Passwort falsch" → verrät nicht, welche E-Mails existieren
        if (user is null
            || passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
        {
            return Problem(detail: "E-Mail oder Passwort ist falsch.", statusCode: StatusCodes.Status401Unauthorized);
        }

        var (token, expiresAt) = tokenService.CreateUserToken(user);
        return Ok(new LoginResponseDto(token, expiresAt, user.ToDto()));
    }
}

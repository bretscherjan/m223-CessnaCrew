using CessnaCrew.WebApi.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CessnaCrew.WebApi.Infrastructure;

/// <summary>
/// Gemeinsame Basis aller Controller: Zugriff auf die Datenbank und Berechtigungs-Hilfen,
/// die mehrere Controller brauchen.
/// </summary>
public abstract class ApiControllerBase(CessnaCrewDbContext db) : ControllerBase
{
    protected CessnaCrewDbContext Db { get; } = db;

    /// <summary>Id des angemeldeten Nutzers (null bei Token-Gästen).</summary>
    protected int? CurrentUserId => User.GetUserId();

    /// <summary>
    /// Lesezugriff auf einen Törn: alle registrierten Nutzer (Owner, RegularGuest)
    /// sowie Token-Gäste, aber nur für ihren eigenen Törn.
    /// </summary>
    protected bool CanReadTurn(int turnId) =>
        CurrentUserId is not null || User.GetGuestTurnId() == turnId;

    /// <summary>
    /// Gehört der aktuelle Benutzer zur Crew des Törns?
    /// Token-Gast für diesen Törn oder registrierter Nutzer mit CrewMember-Eintrag.
    /// </summary>
    protected async Task<bool> IsCrewOfTurnAsync(int turnId)
    {
        if (User.GetGuestTurnId() == turnId)
            return true;

        return CurrentUserId is int userId
            && await Db.CrewMembers.AnyAsync(c => c.TurnId == turnId && c.UserId == userId);
    }

    /// <summary>Kurzform für eine 409-Antwort mit Fehlermeldung.</summary>
    protected ObjectResult ConflictProblem(string detail) =>
        Problem(detail: detail, statusCode: StatusCodes.Status409Conflict);

    /// <summary>Kurzform für eine 400-Antwort, wie bei fehlgeschlagener Validierung.</summary>
    protected ActionResult ValidationError(string field, string message)
    {
        ModelState.AddModelError(field, message);
        return ValidationProblem(ModelState);
    }
}

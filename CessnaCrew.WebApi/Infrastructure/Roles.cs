using CessnaCrew.WebApi.Models;

namespace CessnaCrew.WebApi.Infrastructure;

/// <summary>
/// Rollennamen für [Authorize(Roles = ...)].
/// Owner und RegularGuest entsprechen dem Enum UserRole, Crew ist die Rolle im Gast-Token.
/// </summary>
public static class Roles
{
    public const string Owner = nameof(UserRole.Owner);
    public const string RegularGuest = nameof(UserRole.RegularGuest);
    public const string Crew = "Crew";

    public const string OwnerOrRegularGuest = Owner + "," + RegularGuest;
}

/// <summary>
/// Namen der Claims, die im JWT-Token stehen.
/// </summary>
public static class AppClaims
{
    public const string UserId = "sub";                  // nur bei registrierten Nutzern
    public const string Name = "name";
    public const string Role = "role";
    public const string TurnId = "turn_id";              // nur im Gast-Token
    public const string CrewMemberId = "crew_member_id"; // nur im Gast-Token
}

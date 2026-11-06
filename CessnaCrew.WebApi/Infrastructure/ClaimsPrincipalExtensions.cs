using System.Security.Claims;

namespace CessnaCrew.WebApi.Infrastructure;

/// <summary>
/// Liest die eigenen Claims aus dem angemeldeten Benutzer (this.User im Controller).
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>Id des registrierten Nutzers, oder null bei einem Token-Gast.</summary>
    public static int? GetUserId(this ClaimsPrincipal user) => ReadInt(user, AppClaims.UserId);

    /// <summary>Törn, für den das Gast-Token gilt, oder null bei registrierten Nutzern.</summary>
    public static int? GetGuestTurnId(this ClaimsPrincipal user) => ReadInt(user, AppClaims.TurnId);

    /// <summary>CrewMember-Eintrag des Token-Gasts.</summary>
    public static int? GetCrewMemberId(this ClaimsPrincipal user) => ReadInt(user, AppClaims.CrewMemberId);

    public static bool IsOwner(this ClaimsPrincipal user) => user.IsInRole(Roles.Owner);

    private static int? ReadInt(ClaimsPrincipal user, string claimType) =>
        int.TryParse(user.FindFirstValue(claimType), out var value) ? value : null;
}

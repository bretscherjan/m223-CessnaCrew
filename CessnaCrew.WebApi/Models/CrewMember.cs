namespace CessnaCrew.WebApi.Models;

/// <summary>
/// Teilnehmer eines Törns – registrierter Nutzer (UserId gesetzt) oder anonymer Gast (UserId = null).
/// </summary>
public class CrewMember
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public RoleOnBoard RoleOnBoard { get; set; }

    public int TurnId { get; set; }                                // FK → Turn
    public Turn Turn { get; set; } = null!;

    public int? UserId { get; set; }                               // FK → User, nullable
    public User? User { get; set; }
}

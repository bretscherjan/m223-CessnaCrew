namespace CessnaCrew.WebApi.Models;

/// <summary>
/// Blockiert einen Boots-Slot (Törn oder Wartung).
/// </summary>
public class Turn
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartDateTime { get; set; }
    public DateTime EndDateTime { get; set; }
    public TurnType Type { get; set; }
    public string? InviteToken { get; set; }                       // Unique, null solange kein Link erzeugt wurde
    public bool IsTokenActive { get; set; }

    public int OwnerId { get; set; }                               // FK → User
    public User Owner { get; set; } = null!;

    public ICollection<CrewMember> CrewMembers { get; set; } = new List<CrewMember>(); // 1:n
    public Logbook? Logbook { get; set; }                          // 1:1
}

namespace CessnaCrew.WebApi.Models;

/// <summary>
/// Tausch-Anfrage eines Eigners für einen belegten Törn-Slot.
/// </summary>
public class SwapRequest
{
    public int Id { get; set; }

    public int RequestedTurnId { get; set; }                       // FK → Turn (angefragter Slot)
    public Turn RequestedTurn { get; set; } = null!;

    public int? OfferedTurnId { get; set; }                        // FK → Turn, nullable (angebotener eigener Törn)
    public Turn? OfferedTurn { get; set; }

    public int RequesterId { get; set; }                           // FK → User (Owner)
    public User Requester { get; set; } = null!;

    public string Message { get; set; } = string.Empty;
    public SwapRequestStatus Status { get; set; } = SwapRequestStatus.Open;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

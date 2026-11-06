namespace CessnaCrew.WebApi.Models;

/// <summary>
/// Authentifizierter Account mit Rolle (Owner oder RegularGuest).
/// </summary>
public class User
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;              // Unique
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation Properties (keine eigenen Spalten, nur für EF Core)
    public ICollection<Turn> OrganizedTurns { get; set; } = new List<Turn>();        // organizes
    public ICollection<CrewMember> CrewMemberships { get; set; } = new List<CrewMember>(); // links to (optional)
    public ICollection<Invoice> PaidInvoices { get; set; } = new List<Invoice>();     // pays
    public ICollection<InvoiceSplit> InvoiceSplits { get; set; } = new List<InvoiceSplit>(); // owes
    public ICollection<LogbookRow> CreatedLogbookRows { get; set; } = new List<LogbookRow>(); // created by (optional)
    public ICollection<SwapRequest> SwapRequests { get; set; } = new List<SwapRequest>();
}

namespace CessnaCrew.WebApi.Models;

/// <summary>
/// Anteil eines Eigners an einer Rechnung.
/// </summary>
public class InvoiceSplit
{
    public int Id { get; set; }

    public int InvoiceId { get; set; }                             // FK → Invoice
    public Invoice Invoice { get; set; } = null!;

    public int UserId { get; set; }                                // FK → User (Eigner, der den Anteil schuldet)
    public User User { get; set; } = null!;

    public decimal ShareAmount { get; set; }
    public bool IsSettled { get; set; }
}

namespace CessnaCrew.WebApi.Models;

/// <summary>
/// Rechnung, die ein Eigner bezahlt hat und unter den Eignern aufgeteilt wird.
/// </summary>
public class Invoice
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateOnly InvoiceDate { get; set; }                      // im ERD: date (ohne Uhrzeit)

    public int PayerId { get; set; }                               // FK → User (Eigner, der bezahlt hat)
    public User Payer { get; set; } = null!;

    public ICollection<InvoiceSplit> Splits { get; set; } = new List<InvoiceSplit>(); // 1:n
}

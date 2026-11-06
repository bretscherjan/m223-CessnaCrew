namespace CessnaCrew.WebApi.Models;

/// <summary>
/// Ein Logbuch-Eintrag mit nautischen Messwerten.
/// </summary>
public class LogbookRow
{
    public int Id { get; set; }

    public int LogbookId { get; set; }                             // FK → Logbook
    public Logbook Logbook { get; set; } = null!;

    public DateTime LoggedAt { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public int? WindForceBft { get; set; }
    public string? WindDirection { get; set; }
    public int? AirPressureHpa { get; set; }
    public string? SailConfiguration { get; set; }
    public string? Remarks { get; set; }

    public int? CreatedByUserId { get; set; }                      // FK → User, nullable (anonyme Gäste)
    public User? CreatedByUser { get; set; }
}

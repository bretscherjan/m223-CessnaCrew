namespace CessnaCrew.WebApi.Models;

/// <summary>
/// Logbuch eines Törns (genau eines pro Törn).
/// </summary>
public class Logbook
{
    public int Id { get; set; }

    public int TurnId { get; set; }                                // FK → Turn, Unique (1:1)
    public Turn Turn { get; set; } = null!;

    public bool IsHighSeaMode { get; set; }
    public decimal? EngineHoursStart { get; set; }
    public decimal? EngineHoursEnd { get; set; }
    public bool IsClosed { get; set; }

    public ICollection<LogbookRow> Rows { get; set; } = new List<LogbookRow>(); // 1:n
}

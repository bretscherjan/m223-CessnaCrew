using System.ComponentModel.DataAnnotations;

namespace CessnaCrew.WebApi.Dto;

public record LogbookDto(
    int Id,
    int TurnId,
    bool IsHighSeaMode,
    decimal? EngineHoursStart,
    decimal? EngineHoursEnd,
    bool IsClosed,
    IReadOnlyList<LogbookRowDto> Rows
);

public record LogbookRowDto(
    int Id,
    DateTime LoggedAt,
    decimal? Latitude,
    decimal? Longitude,
    int? WindForceBft,
    string? WindDirection,
    int? AirPressureHpa,
    string? SailConfiguration,
    string? Remarks,
    int? CreatedByUserId
);

// PUT /api/turns/{id}/logbook
public record UpdateLogbookDto(
    bool IsHighSeaMode,
    [Range(0, 100000)] decimal? EngineHoursStart,
    [Range(0, 100000)] decimal? EngineHoursEnd
);

// POST /api/turns/{id}/logbook/rows und PUT .../rows/{rowId}
public record SaveLogbookRowDto(
    [Required] DateTime? LoggedAt,
    [Range(-90, 90)] decimal? Latitude,
    [Range(-180, 180)] decimal? Longitude,
    [Range(0, 12)] int? WindForceBft,
    [StringLength(10)] string? WindDirection,
    [Range(850, 1100)] int? AirPressureHpa,
    [StringLength(100)] string? SailConfiguration,
    [StringLength(1000)] string? Remarks
);

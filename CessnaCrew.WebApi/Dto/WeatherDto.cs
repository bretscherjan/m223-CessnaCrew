namespace CessnaCrew.WebApi.Dto;

// GET /api/weather?lat=..&lon=..
public record WeatherDto(
    decimal Latitude,
    decimal Longitude,
    DateTime ObservedAt,
    decimal TemperatureC,
    decimal WindSpeedKn,
    int WindForceBft,
    int WindDirectionDeg,
    string WindDirection,           // z. B. "SW" – passt direkt ins Logbuch
    int AirPressureHpa
);

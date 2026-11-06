using System.Globalization;
using System.Text.Json.Serialization;
using CessnaCrew.WebApi.Dto;
using CessnaCrew.WebApi.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

namespace CessnaCrew.WebApi.Controllers;

/// <summary>
/// 5 Logbuch – aktuelles Wetter zur Position. Proxy zu Open-Meteo (kostenlos, kein API-Key nötig).
/// </summary>
[ApiController]
[Authorize]                 // Owner und Crew. Ohne Törn-Bezug lässt sich "Crew" hier nicht genauer prüfen.
[Produces(MediaTypeNames.Application.Json)]
public class WeatherController(IHttpClientFactory httpClientFactory) : ControllerBase
{
    public const string HttpClientName = "OpenMeteo";

    // GET /api/weather?lat=47.65&lon=9.18
    [HttpGet(StaticRoutes.Weather.Get)]
    [ProducesResponseType<WeatherDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<WeatherDto>> GetWeather(
        [FromQuery, Required, Range(-90, 90)] decimal? lat,
        [FromQuery, Required, Range(-180, 180)] decimal? lon,
        CancellationToken cancellationToken)
    {
        // InvariantCulture: Dezimalpunkt statt Komma in der URL
        var url = string.Create(CultureInfo.InvariantCulture,
            $"v1/forecast?latitude={lat}&longitude={lon}&current=temperature_2m,wind_speed_10m,wind_direction_10m,surface_pressure&wind_speed_unit=kn");

        OpenMeteoResponse? response;
        try
        {
            var client = httpClientFactory.CreateClient(HttpClientName);
            response = await client.GetFromJsonAsync<OpenMeteoResponse>(url, cancellationToken);
        }
        catch (HttpRequestException)
        {
            response = null;
        }

        if (response?.Current is null)
            return Problem(detail: "Der Wetterdienst ist nicht erreichbar.", statusCode: StatusCodes.Status502BadGateway);

        var c = response.Current;
        return Ok(new WeatherDto(
            Latitude: lat!.Value,
            Longitude: lon!.Value,
            ObservedAt: c.Time,
            TemperatureC: c.Temperature,
            WindSpeedKn: c.WindSpeed,
            WindForceBft: ToBeaufort(c.WindSpeed),
            WindDirectionDeg: c.WindDirection,
            WindDirection: ToCompass(c.WindDirection),
            AirPressureHpa: (int)Math.Round(c.SurfacePressure)));
    }

    /// <summary>Knoten → Beaufort (obere Grenzen der Stufen 0 bis 11).</summary>
    private static int ToBeaufort(decimal knots)
    {
        decimal[] limits = [1, 4, 7, 11, 17, 22, 28, 34, 41, 48, 56, 64];
        return limits.Count(limit => knots >= limit);
    }

    /// <summary>Grad → Himmelsrichtung (16 Sektoren à 22.5°).</summary>
    private static string ToCompass(int degrees)
    {
        string[] directions = ["N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"];
        return directions[(int)Math.Round(((degrees % 360) + 360) % 360 / 22.5) % 16];
    }

    // Ausschnitt der Antwort von Open-Meteo
    private record OpenMeteoResponse([property: JsonPropertyName("current")] OpenMeteoCurrent? Current);

    private record OpenMeteoCurrent(
        [property: JsonPropertyName("time")] DateTime Time,
        [property: JsonPropertyName("temperature_2m")] decimal Temperature,
        [property: JsonPropertyName("wind_speed_10m")] decimal WindSpeed,
        [property: JsonPropertyName("wind_direction_10m")] int WindDirection,
        [property: JsonPropertyName("surface_pressure")] decimal SurfacePressure);
}

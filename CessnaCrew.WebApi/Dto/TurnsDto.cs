using System.ComponentModel.DataAnnotations;
using CessnaCrew.WebApi.Models;

namespace CessnaCrew.WebApi.Dto;

// Antwort für Kalender und Historie
public record TurnDto(
    int Id,
    string Title,
    DateTime StartDateTime,
    DateTime EndDateTime,
    TurnType Type,
    int OwnerId,
    string OwnerName,
    bool IsCompleted
);

// GET /api/turns/{id} – Details inkl. Crew
public record TurnDetailDto(
    int Id,
    string Title,
    DateTime StartDateTime,
    DateTime EndDateTime,
    TurnType Type,
    int OwnerId,
    string OwnerName,
    bool IsCompleted,
    bool IsInviteLinkActive,
    IReadOnlyList<CrewMemberDto> Crew
);

// POST /api/turns und PUT /api/turns/{id}
public record SaveTurnDto(
    [Required, StringLength(100)] string Title,
    [Required] DateTime? StartDateTime,
    [Required] DateTime? EndDateTime,
    [Required] TurnType? Type
);

// POST /api/turns/{id}/complete – Body optional
public record CompleteTurnDto(
    [Range(0, 100000)] decimal? EngineHoursEnd
);

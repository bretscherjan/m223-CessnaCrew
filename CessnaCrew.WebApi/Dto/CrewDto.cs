using System.ComponentModel.DataAnnotations;
using CessnaCrew.WebApi.Models;

namespace CessnaCrew.WebApi.Dto;

public record CrewMemberDto(
    int Id,
    string DisplayName,
    RoleOnBoard RoleOnBoard,
    int? UserId                     // null = anonymer Gast über Einladungs-Link
);

// POST /api/turns/{id}/crew
public record AddCrewMemberDto(
    [Required] int? UserId,
    [Required] RoleOnBoard? RoleOnBoard
);

// PUT /api/turns/{id}/crew/{memberId}
public record UpdateCrewMemberDto(
    [Required] RoleOnBoard? RoleOnBoard
);

// POST /api/turns/{id}/invite-token
public record InviteTokenDto(
    string Token,
    bool IsActive
);

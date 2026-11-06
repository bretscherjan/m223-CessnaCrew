using System.ComponentModel.DataAnnotations;

namespace CessnaCrew.WebApi.Dto;

// GET /api/invites/{token} – Törn-Eckdaten für den Gast
public record InviteDto(
    int TurnId,
    string TurnTitle,
    DateTime StartDateTime,
    DateTime EndDateTime,
    string OwnerName
);

// POST /api/invites/{token}/join
public record JoinInviteDto(
    [Required, StringLength(100, MinimumLength = 2)] string DisplayName
);

public record GuestTokenDto(
    string Token,
    DateTime ExpiresAt,
    CrewMemberDto CrewMember,
    int TurnId
);

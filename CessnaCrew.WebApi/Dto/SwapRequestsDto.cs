using System.ComponentModel.DataAnnotations;
using CessnaCrew.WebApi.Models;

namespace CessnaCrew.WebApi.Dto;

public record SwapRequestDto(
    int Id,
    TurnDto RequestedTurn,
    TurnDto? OfferedTurn,
    int RequesterId,
    string RequesterName,
    string Message,
    SwapRequestStatus Status,
    DateTime CreatedAt,
    bool IsIncoming                 // true = Anfrage an mich, false = von mir gestellt
);

// POST /api/turns/{id}/swap-requests
public record CreateSwapRequestDto(
    int? OfferedTurnId,
    [StringLength(1000)] string? Message
);

// PUT /api/swap-requests/{id}
public record AnswerSwapRequestDto(
    [Required] SwapRequestStatus? Status   // Accepted oder Rejected
);

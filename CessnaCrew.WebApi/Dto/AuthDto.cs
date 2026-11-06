using System.ComponentModel.DataAnnotations;
using CessnaCrew.WebApi.Models;

namespace CessnaCrew.WebApi.Dto;

// POST /api/auth/register  → Antwort: UserDto
public record RegisterRequestDto(
    [Required, StringLength(50, MinimumLength = 3)] string Username,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, MinLength(8)] string Password,
    [Required] UserRole Role
);

// POST /api/auth/login
public record LoginRequestDto(
    [Required, EmailAddress] string Email,
    [Required] string Password          // keine MinLength: falsches Passwort → 401, nicht 400
);

public record LoginResponseDto(
    string Token,
    DateTime ExpiresAt,
    UserDto User
);

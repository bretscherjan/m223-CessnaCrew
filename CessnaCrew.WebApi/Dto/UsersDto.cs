using System.ComponentModel.DataAnnotations;
using CessnaCrew.WebApi.Models;

namespace CessnaCrew.WebApi.Dto;

// Antwort für alle User-Endpunkte (ohne PasswordHash!)
public record UserDto(
    int Id,
    string Username,
    string Email,
    UserRole Role,
    DateTime CreatedAt
);

// PUT /api/users/{id}
public record UpdateUserDto(
    [Required, StringLength(50, MinimumLength = 3)] string Username,
    [Required, EmailAddress, StringLength(256)] string Email,
    [MinLength(8)] string? Password     // optional: null = Passwort bleibt gleich
);

using System.Security.Claims;
using System.Text;
using CessnaCrew.WebApi.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace CessnaCrew.WebApi.Infrastructure;

/// <summary>
/// Einstellungen aus appsettings.json, Abschnitt "Jwt".
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;       // mind. 32 Zeichen (256 Bit) für HS256
    public int ExpiresInHours { get; set; } = 8;
    public int GuestExpiresInDays { get; set; } = 30;

    public SymmetricSecurityKey GetSigningKey() => new(Encoding.UTF8.GetBytes(Key));
}

/// <summary>
/// Stellt die JWT-Tokens aus: für angemeldete Nutzer (Login) und für Gäste (Einladungs-Link).
/// </summary>
public class TokenService(IOptions<JwtOptions> options)
{
    private readonly JwtOptions _options = options.Value;

    public (string Token, DateTime ExpiresAt) CreateUserToken(User user)
    {
        var claims = new Dictionary<string, object>
        {
            [AppClaims.UserId] = user.Id.ToString(),
            [AppClaims.Name] = user.Username,
            [AppClaims.Role] = user.Role.ToString()
        };
        return CreateToken(claims, DateTime.UtcNow.AddHours(_options.ExpiresInHours));
    }

    /// <summary>
    /// Gast-Token: gilt nur für einen Törn. Mit dem Törn-Abschluss wird es zusätzlich
    /// serverseitig ungültig (Prüfung in Program.cs, OnTokenValidated).
    /// </summary>
    public (string Token, DateTime ExpiresAt) CreateGuestToken(CrewMember member)
    {
        var claims = new Dictionary<string, object>
        {
            [AppClaims.Name] = member.DisplayName,
            [AppClaims.Role] = Roles.Crew,
            [AppClaims.TurnId] = member.TurnId.ToString(),
            [AppClaims.CrewMemberId] = member.Id.ToString()
        };
        return CreateToken(claims, DateTime.UtcNow.AddDays(_options.GuestExpiresInDays));
    }

    private (string Token, DateTime ExpiresAt) CreateToken(Dictionary<string, object> claims, DateTime expiresAt)
    {
        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Claims = claims,
            Expires = expiresAt,
            SigningCredentials = new SigningCredentials(_options.GetSigningKey(), SecurityAlgorithms.HmacSha256)
        };
        return (new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}

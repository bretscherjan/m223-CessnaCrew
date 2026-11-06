using System.Text.Json.Serialization;
using CessnaCrew.WebApi.Controllers;
using CessnaCrew.WebApi.Data;
using CessnaCrew.WebApi.Infrastructure;
using CessnaCrew.WebApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------- Datenbank (PostgreSQL, Connection-String in appsettings.json) ----------
builder.Services.AddDbContext<CessnaCrewDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("CessnaCrew")));

// ---------- Authentifizierung mit JWT ----------
var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtOptions = jwtSection.Get<JwtOptions>()
    ?? throw new InvalidOperationException("Abschnitt 'Jwt' fehlt in appsettings.json.");
if (jwtOptions.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key muss mindestens 32 Zeichen lang sein (appsettings.Development.json oder Umgebungsvariable Jwt__Key).");
builder.Services.Configure<JwtOptions>(jwtSection);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;          // Claim-Namen unverändert lassen ("sub", "role", ...)
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = jwtOptions.GetSigningKey(),
            NameClaimType = AppClaims.Name,
            RoleClaimType = AppClaims.Role
        };
        options.Events = new JwtBearerEvents
        {
            // Gast-Tokens verfallen sofort, wenn der Törn abgeschlossen oder der Gast entfernt wurde
            OnTokenValidated = async context =>
            {
                if (context.Principal?.IsInRole(Roles.Crew) != true)
                    return;

                var crewMemberId = context.Principal.GetCrewMemberId();
                var db = context.HttpContext.RequestServices.GetRequiredService<CessnaCrewDbContext>();
                var stillValid = await db.CrewMembers.AnyAsync(c =>
                    c.Id == crewMemberId && !(c.Turn.Logbook != null && c.Turn.Logbook.IsClosed));

                if (!stillValid)
                    context.Fail("Das Gast-Token ist abgelaufen.");
            }
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();
builder.Services.AddSingleton<TokenService>();

// ---------- Externer Wetterdienst ----------
builder.Services.AddHttpClient(WeatherController.HttpClientName, client =>
{
    client.BaseAddress = new Uri("https://api.open-meteo.com/");
    client.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Ausstehende Migrationen beim Start automatisch anwenden (nur Entwicklung)
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<CessnaCrewDbContext>().Database.Migrate();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

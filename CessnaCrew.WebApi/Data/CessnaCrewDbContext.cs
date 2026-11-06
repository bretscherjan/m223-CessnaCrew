using CessnaCrew.WebApi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CessnaCrew.WebApi.Data;

/// <summary>
/// Datenbank-Kontext: jede DbSet-Property wird zu einer Tabelle.
/// Die Struktur entspricht dem ERD (Dokumentation, Kapitel 2).
/// </summary>
public class CessnaCrewDbContext(DbContextOptions<CessnaCrewDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Turn> Turns => Set<Turn>();
    public DbSet<CrewMember> CrewMembers => Set<CrewMember>();
    public DbSet<Logbook> Logbooks => Set<Logbook>();
    public DbSet<LogbookRow> LogbookRows => Set<LogbookRow>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceSplit> InvoiceSplits => Set<InvoiceSplit>();
    public DbSet<SwapRequest> SwapRequests => Set<SwapRequest>();

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // PostgreSQL: DateTime als "timestamp without time zone" speichern.
        // Törn-Zeiten sind Ortszeit am Bodensee und werden so unverändert gespeichert.
        // Npgsql akzeptiert dafür nur DateTimeKind.Unspecified – der Converter entfernt die
        // Zeitzonen-Markierung (z. B. bei DateTime.UtcNow oder "...Z" vom Client).
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("timestamp without time zone")
            .HaveConversion<UnspecifiedKindConverter>();
    }

    private class UnspecifiedKindConverter() : ValueConverter<DateTime, DateTime>(
        v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
        v => v);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------- USER ----------
        modelBuilder.Entity<User>(e =>
        {
            e.Property(u => u.Username).HasMaxLength(50).IsRequired();
            e.Property(u => u.Email).HasMaxLength(256).IsRequired();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        // ---------- TURN ----------
        modelBuilder.Entity<Turn>(e =>
        {
            e.Property(t => t.Title).HasMaxLength(100).IsRequired();
            e.Property(t => t.Type).HasConversion<string>().HasMaxLength(20);
            e.Property(t => t.InviteToken).HasMaxLength(64);
            e.HasIndex(t => t.InviteToken).IsUnique();   // mehrere NULL-Werte sind erlaubt

            // USER 1:n TURN (organizes)
            e.HasOne(t => t.Owner)
             .WithMany(u => u.OrganizedTurns)
             .HasForeignKey(t => t.OwnerId)
             .OnDelete(DeleteBehavior.Restrict);         // User mit Törns kann nicht gelöscht werden
        });

        // ---------- CREW_MEMBER ----------
        modelBuilder.Entity<CrewMember>(e =>
        {
            e.Property(c => c.DisplayName).HasMaxLength(100).IsRequired();
            e.Property(c => c.RoleOnBoard).HasConversion<string>().HasMaxLength(20);

            // TURN 1:n CREW_MEMBER (has)
            e.HasOne(c => c.Turn)
             .WithMany(t => t.CrewMembers)
             .HasForeignKey(c => c.TurnId)
             .OnDelete(DeleteBehavior.Cascade);          // Törn gelöscht → Crew weg

            // USER 1:n CREW_MEMBER (links to, optional)
            e.HasOne(c => c.User)
             .WithMany(u => u.CrewMemberships)
             .HasForeignKey(c => c.UserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ---------- LOGBOOK ----------
        modelBuilder.Entity<Logbook>(e =>
        {
            e.Property(l => l.EngineHoursStart).HasPrecision(10, 2);
            e.Property(l => l.EngineHoursEnd).HasPrecision(10, 2);

            // TURN 1:1 LOGBOOK (has) – erzeugt automatisch einen Unique-Index auf TurnId
            e.HasOne(l => l.Turn)
             .WithOne(t => t.Logbook)
             .HasForeignKey<Logbook>(l => l.TurnId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ---------- LOGBOOK_ROW ----------
        modelBuilder.Entity<LogbookRow>(e =>
        {
            e.Property(r => r.Latitude).HasPrecision(9, 6);
            e.Property(r => r.Longitude).HasPrecision(9, 6);
            e.Property(r => r.WindDirection).HasMaxLength(10);
            e.Property(r => r.SailConfiguration).HasMaxLength(100);
            e.Property(r => r.Remarks).HasMaxLength(1000);

            // LOGBOOK 1:n LOGBOOK_ROW (contains)
            e.HasOne(r => r.Logbook)
             .WithMany(l => l.Rows)
             .HasForeignKey(r => r.LogbookId)
             .OnDelete(DeleteBehavior.Cascade);

            // USER 1:n LOGBOOK_ROW (created by, optional)
            e.HasOne(r => r.CreatedByUser)
             .WithMany(u => u.CreatedLogbookRows)
             .HasForeignKey(r => r.CreatedByUserId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ---------- INVOICE ----------
        modelBuilder.Entity<Invoice>(e =>
        {
            e.Property(i => i.Title).HasMaxLength(100).IsRequired();
            e.Property(i => i.TotalAmount).HasPrecision(18, 2);

            // USER 1:n INVOICE (pays)
            e.HasOne(i => i.Payer)
             .WithMany(u => u.PaidInvoices)
             .HasForeignKey(i => i.PayerId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- INVOICE_SPLIT ----------
        modelBuilder.Entity<InvoiceSplit>(e =>
        {
            e.Property(s => s.ShareAmount).HasPrecision(18, 2);

            // INVOICE 1:n INVOICE_SPLIT (divides)
            e.HasOne(s => s.Invoice)
             .WithMany(i => i.Splits)
             .HasForeignKey(s => s.InvoiceId)
             .OnDelete(DeleteBehavior.Cascade);          // Rechnung gelöscht → Anteile weg

            // USER 1:n INVOICE_SPLIT (owes)
            e.HasOne(s => s.User)
             .WithMany(u => u.InvoiceSplits)
             .HasForeignKey(s => s.UserId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- SWAP_REQUEST ----------
        modelBuilder.Entity<SwapRequest>(e =>
        {
            e.Property(s => s.Message).HasMaxLength(1000);
            e.Property(s => s.Status).HasConversion<string>().HasMaxLength(20);

            // Angefragter Slot
            e.HasOne(s => s.RequestedTurn)
             .WithMany()
             .HasForeignKey(s => s.RequestedTurnId)
             .OnDelete(DeleteBehavior.Cascade);

            // Angebotener Slot (optional). NoAction, damit ein gelöschter Törn nicht still
            // fremde Anfragen löscht (TurnsController räumt diese explizit auf).
            e.HasOne(s => s.OfferedTurn)
             .WithMany()
             .HasForeignKey(s => s.OfferedTurnId)
             .OnDelete(DeleteBehavior.NoAction);

            e.HasOne(s => s.Requester)
             .WithMany(u => u.SwapRequests)
             .HasForeignKey(s => s.RequesterId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

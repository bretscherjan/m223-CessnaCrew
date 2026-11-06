using CessnaCrew.WebApi.Data;
using CessnaCrew.WebApi.Dto;
using CessnaCrew.WebApi.Infrastructure;
using CessnaCrew.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mime;

namespace CessnaCrew.WebApi.Controllers;

/// <summary>
/// 6 Kosten-Splitter – ausschliesslich für Eigner.
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Owner)]
[Consumes(MediaTypeNames.Application.Json)]
[Produces(MediaTypeNames.Application.Json)]
public class InvoicesController(CessnaCrewDbContext db) : ApiControllerBase(db)
{
    // GET /api/invoices?year=2026
    [HttpGet(StaticRoutes.Invoices.GetAll)]
    [ProducesResponseType<IEnumerable<InvoiceDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<InvoiceDto>>> GetInvoices([FromQuery] int? year)
    {
        var query = IncludeAll();
        if (year is not null)
            query = query.Where(i => i.InvoiceDate.Year == year);

        var invoices = await query.OrderByDescending(i => i.InvoiceDate).ToListAsync();
        return Ok(invoices.Select(i => i.ToDto()));
    }

    // GET /api/invoices/{id}
    [HttpGet(StaticRoutes.Invoices.ById)]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceDto>> GetInvoice([FromRoute] int id)
    {
        var invoice = await LoadAsync(id);
        if (invoice is null)
            return NotFound();

        return Ok(invoice.ToDto());
    }

    // POST /api/invoices
    [HttpPost(StaticRoutes.Invoices.GetAll)]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InvoiceDto>> CreateInvoice([FromBody] SaveInvoiceDto request)
    {
        var payerId = CurrentUserId!.Value;               // Zahler = angemeldeter Eigner

        var (splits, error) = await BuildSplitsAsync(request, payerId);
        if (error is not null)
            return ValidationError(nameof(request.Shares), error);

        var invoice = new Invoice
        {
            Title = request.Title.Trim(),
            TotalAmount = request.TotalAmount!.Value,
            InvoiceDate = request.InvoiceDate!.Value,
            PayerId = payerId,
            Splits = splits
        };

        Db.Invoices.Add(invoice);
        await Db.SaveChangesAsync();

        var created = await LoadAsync(invoice.Id);
        return Created($"/{StaticRoutes.Invoices.Base}/{invoice.Id}", created!.ToDto());
    }

    // PUT /api/invoices/{id}
    [HttpPut(StaticRoutes.Invoices.ById)]
    [ProducesResponseType<InvoiceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceDto>> UpdateInvoice([FromRoute] int id, [FromBody] SaveInvoiceDto request)
    {
        var invoice = await Db.Invoices.Include(i => i.Splits).SingleOrDefaultAsync(i => i.Id == id);
        if (invoice is null)
            return NotFound();
        if (invoice.PayerId != CurrentUserId)
            return Forbid();                               // nur der Zahler

        var (splits, error) = await BuildSplitsAsync(request, invoice.PayerId);
        if (error is not null)
            return ValidationError(nameof(request.Shares), error);

        invoice.Title = request.Title.Trim();
        invoice.TotalAmount = request.TotalAmount!.Value;
        invoice.InvoiceDate = request.InvoiceDate!.Value;

        // Anteile werden komplett neu berechnet
        Db.InvoiceSplits.RemoveRange(invoice.Splits);
        invoice.Splits = splits;

        await Db.SaveChangesAsync();

        var updated = await LoadAsync(id);
        return Ok(updated!.ToDto());
    }

    // DELETE /api/invoices/{id}
    [HttpDelete(StaticRoutes.Invoices.ById)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteInvoice([FromRoute] int id)
    {
        var invoice = await Db.Invoices.FindAsync(id);
        if (invoice is null)
            return NotFound();
        if (invoice.PayerId != CurrentUserId)
            return Forbid();

        Db.Invoices.Remove(invoice);                       // Anteile per Cascade
        await Db.SaveChangesAsync();
        return NoContent();
    }

    // PUT /api/invoices/{id}/splits/{splitId}
    [HttpPut(StaticRoutes.Invoices.SplitById)]
    [ProducesResponseType<InvoiceSplitDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InvoiceSplitDto>> UpdateSplit(
        [FromRoute] int id, [FromRoute] int splitId, [FromBody] UpdateInvoiceSplitDto request)
    {
        var split = await Db.InvoiceSplits.Include(s => s.User)
            .SingleOrDefaultAsync(s => s.Id == splitId && s.InvoiceId == id);
        if (split is null)
            return NotFound();

        split.IsSettled = request.IsSettled;
        await Db.SaveChangesAsync();
        return Ok(split.ToDto());
    }

    /// <summary>
    /// Erstellt die Anteile: entweder wie angegeben (Summe muss stimmen)
    /// oder gleichmässig unter allen Eignern. Rundungsrappen gehen an den Zahler.
    /// Der Anteil des Zahlers ist automatisch beglichen.
    /// </summary>
    private async Task<(List<InvoiceSplit> Splits, string? Error)> BuildSplitsAsync(SaveInvoiceDto request, int payerId)
    {
        var total = request.TotalAmount!.Value;
        var ownerIds = await Db.Users.Where(u => u.Role == UserRole.Owner).Select(u => u.Id).ToListAsync();

        Dictionary<int, decimal> shares;

        if (request.Shares is null || request.Shares.Count == 0)
        {
            // Gleichmässig aufteilen, auf Rappen abgerundet
            var each = Math.Floor(total / ownerIds.Count * 100) / 100;
            shares = ownerIds.ToDictionary(userId => userId, _ => each);
            shares[payerId] += total - each * ownerIds.Count;
        }
        else
        {
            var userIds = request.Shares.Select(s => s.UserId!.Value).ToList();

            if (userIds.Distinct().Count() != userIds.Count)
                return ([], "Jeder Eigner darf nur einmal vorkommen.");
            if (userIds.Any(userId => !ownerIds.Contains(userId)))
                return ([], "Kosten dürfen nur unter Eignern aufgeteilt werden.");

            shares = request.Shares.ToDictionary(s => s.UserId!.Value, s => s.ShareAmount!.Value);
            if (shares.Values.Sum() != total)
                return ([], $"Die Summe der Anteile ({shares.Values.Sum():0.00}) entspricht nicht dem Gesamtbetrag ({total:0.00}).");
        }

        var splits = shares.Select(s => new InvoiceSplit
        {
            UserId = s.Key,
            ShareAmount = s.Value,
            IsSettled = s.Key == payerId
        }).ToList();

        return (splits, null);
    }

    private IQueryable<Invoice> IncludeAll() => Db.Invoices
        .Include(i => i.Payer)
        .Include(i => i.Splits).ThenInclude(s => s.User);

    private Task<Invoice?> LoadAsync(int id) => IncludeAll().SingleOrDefaultAsync(i => i.Id == id);
}

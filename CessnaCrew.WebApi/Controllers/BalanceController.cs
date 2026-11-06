using CessnaCrew.WebApi.Data;
using CessnaCrew.WebApi.Dto;
using CessnaCrew.WebApi.Infrastructure;
using CessnaCrew.WebApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Net.Mime;

namespace CessnaCrew.WebApi.Controllers;

/// <summary>
/// 6 Jahresbilanz – ausschliesslich für Eigner.
/// </summary>
[ApiController]
[Authorize(Roles = Roles.Owner)]
[Produces(MediaTypeNames.Application.Json)]
public class BalanceController(CessnaCrewDbContext db) : ApiControllerBase(db)
{
    // GET /api/balance/{year}
    [HttpGet(StaticRoutes.Balance.ByYear)]
    [ProducesResponseType<BalanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BalanceDto>> GetBalance([FromRoute, Range(2000, 2100)] int year)
    {
        var owners = await Db.Users
            .Where(u => u.Role == UserRole.Owner)
            .OrderBy(u => u.Username)
            .ToListAsync();

        var invoices = await Db.Invoices
            .Include(i => i.Splits)
            .Where(i => i.InvoiceDate.Year == year)
            .ToListAsync();

        var splits = invoices.SelectMany(i => i.Splits).ToList();

        var result = owners.Select(owner =>
        {
            var paid = invoices.Where(i => i.PayerId == owner.Id).Sum(i => i.TotalAmount);
            var owed = splits.Where(s => s.UserId == owner.Id).Sum(s => s.ShareAmount);
            var openToPay = splits.Where(s => s.UserId == owner.Id && !s.IsSettled).Sum(s => s.ShareAmount);
            var openToReceive = invoices
                .Where(i => i.PayerId == owner.Id)
                .SelectMany(i => i.Splits)
                .Where(s => !s.IsSettled)
                .Sum(s => s.ShareAmount);

            return new OwnerBalanceDto(owner.Id, owner.Username, paid, owed, paid - owed, openToPay, openToReceive);
        }).ToList();

        return Ok(new BalanceDto(year, result));
    }
}

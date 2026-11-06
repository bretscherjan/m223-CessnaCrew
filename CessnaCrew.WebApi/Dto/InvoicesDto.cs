using System.ComponentModel.DataAnnotations;

namespace CessnaCrew.WebApi.Dto;

public record InvoiceDto(
    int Id,
    string Title,
    decimal TotalAmount,
    DateOnly InvoiceDate,
    int PayerId,
    string PayerName,
    IReadOnlyList<InvoiceSplitDto> Splits
);

public record InvoiceSplitDto(
    int Id,
    int UserId,
    string Username,
    decimal ShareAmount,
    bool IsSettled
);

// POST /api/invoices und PUT /api/invoices/{id}
public record SaveInvoiceDto(
    [Required, StringLength(100)] string Title,
    [Required, Range(0.01, 1_000_000)] decimal? TotalAmount,
    [Required] DateOnly? InvoiceDate,
    List<InvoiceShareDto>? Shares   // leer/null = gleichmässig unter allen Eignern aufteilen
);

public record InvoiceShareDto(
    [Required] int? UserId,
    [Required, Range(0, 1_000_000)] decimal? ShareAmount
);

// PUT /api/invoices/{id}/splits/{splitId}
public record UpdateInvoiceSplitDto(
    bool IsSettled
);

// GET /api/balance/{year}
public record BalanceDto(
    int Year,
    IReadOnlyList<OwnerBalanceDto> Owners
);

public record OwnerBalanceDto(
    int UserId,
    string Username,
    decimal Paid,                   // selbst bezahlte Rechnungen
    decimal Owed,                   // eigener Anteil an allen Rechnungen
    decimal Balance,                // Paid - Owed: positiv = bekommt Geld, negativ = schuldet Geld
    decimal OpenToPay,              // eigene, noch nicht beglichene Anteile
    decimal OpenToReceive           // offene Anteile anderer an eigenen Rechnungen
);

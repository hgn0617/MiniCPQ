using System.ComponentModel.DataAnnotations;
using MiniCPQ.Domain;

namespace MiniCPQ.Application.DTOs;

public sealed record CreateQuoteRequest(
    [Required, StringLength(200)] string CustomerName,
    Guid? ExchangeRateId = null);

public sealed record AddQuoteItemRequest(
    Guid ServerId,
    [Range(1, int.MaxValue)] int Quantity);

public sealed record UpdateQuoteItemRequest(
    [Range(1, int.MaxValue)] int Quantity);

public sealed record ApproveQuoteRequest(
    [Range(typeof(decimal), "0.01", "9999999999999999")] decimal Price);

public sealed record QuoteMaterialSnapshotDto(
    Guid Id,
    Guid? MaterialId,
    string MaterialName,
    string MaterialType,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice);

public sealed record QuoteItemDto(
    Guid Id,
    Guid? ServerId,
    string ServerName,
    int Quantity,
    decimal? UnitCost,
    decimal? TotalCost,
    IReadOnlyCollection<QuoteMaterialSnapshotDto> Materials);

public sealed record QuoteDto(
    Guid Id,
    string CustomerName,
    DateTimeOffset CreatedAtUtc,
    QuoteStatus Status,
    decimal Cost,
    decimal? Price,
    decimal? Profit,
    decimal? GrossMarginPercent,
    string CurrencyCode,
    string CurrencyName,
    decimal CnyPerUnit,
    decimal ForeignCost,
    decimal? ForeignPrice,
    decimal? ForeignProfit,
    string CreatedByUserId,
    IReadOnlyCollection<QuoteItemDto> Items);

using System.ComponentModel.DataAnnotations;

namespace MiniCPQ.Application.DTOs;

public sealed record CreateExchangeRateRequest(
    [Required, StringLength(3, MinimumLength = 3)] string CurrencyCode,
    [Required, StringLength(50)] string CurrencyName,
    [Range(typeof(decimal), "0.000001", "999999999999")] decimal CnyPerUnit);

public sealed record UpdateExchangeRateRequest(
    [Required, StringLength(3, MinimumLength = 3)] string CurrencyCode,
    [Required, StringLength(50)] string CurrencyName,
    [Range(typeof(decimal), "0.000001", "999999999999")] decimal CnyPerUnit,
    Guid Version);

public sealed record ExchangeRateDto(
    Guid Id,
    string CurrencyCode,
    string CurrencyName,
    decimal CnyPerUnit,
    Guid Version);

using System.ComponentModel.DataAnnotations;

namespace MiniCPQ.Application.DTOs;

public sealed record CreateMaterialRequest(
    [Required, StringLength(100)] string Name,
    [Required, StringLength(50)] string Type,
    [Range(typeof(decimal), "0", "9999999999999999")] decimal UnitPrice);

public sealed record UpdateMaterialRequest(
    [Required, StringLength(100)] string Name,
    [Required, StringLength(50)] string Type,
    [Range(typeof(decimal), "0", "9999999999999999")] decimal UnitPrice,
    Guid Version);

public sealed record MaterialDto(
    Guid Id,
    string Name,
    string Type,
    decimal UnitPrice,
    Guid Version);

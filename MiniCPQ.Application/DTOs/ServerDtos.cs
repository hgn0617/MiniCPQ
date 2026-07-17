using System.ComponentModel.DataAnnotations;

namespace MiniCPQ.Application.DTOs;

public sealed record ServerMaterialInput(
    Guid MaterialId,
    [Range(1, int.MaxValue)] int Quantity);

public sealed record CreateServerRequest(
    [Required, StringLength(100)] string Name,
    [MinLength(1)] IReadOnlyCollection<ServerMaterialInput> Materials);

public sealed record UpdateServerRequest(
    [Required, StringLength(100)] string Name,
    [MinLength(1)] IReadOnlyCollection<ServerMaterialInput> Materials);

public sealed record ServerMaterialDto(
    Guid MaterialId,
    string Name,
    string Type,
    decimal UnitPrice,
    int Quantity,
    decimal Subtotal);

public sealed record ServerDto(
    Guid Id,
    string Name,
    decimal CurrentCost,
    IReadOnlyCollection<ServerMaterialDto> Materials);

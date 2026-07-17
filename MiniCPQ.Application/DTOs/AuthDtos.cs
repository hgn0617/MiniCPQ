using System.ComponentModel.DataAnnotations;

namespace MiniCPQ.Application.DTOs;

public sealed record LoginRequest(
    [Required] string Username,
    [Required] string Password,
    bool RememberMe = false);

public sealed record CurrentUserDto(string Id, string Username, string Email, IReadOnlyCollection<string> Roles);

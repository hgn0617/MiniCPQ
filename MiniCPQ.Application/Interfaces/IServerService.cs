using MiniCPQ.Application.DTOs;

namespace MiniCPQ.Application.Interfaces;

public interface IServerService
{
    Task<IReadOnlyCollection<ServerDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<ServerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ServerDto> CreateAsync(CreateServerRequest request, CancellationToken cancellationToken);
    Task<ServerDto> UpdateAsync(Guid id, UpdateServerRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

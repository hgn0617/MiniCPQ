using MiniCPQ.Application.DTOs;

namespace MiniCPQ.Application.Interfaces;

public interface IMaterialService
{
    Task<IReadOnlyCollection<MaterialDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<MaterialDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<MaterialDto> CreateAsync(CreateMaterialRequest request, CancellationToken cancellationToken);
    Task<MaterialDto> UpdateAsync(Guid id, UpdateMaterialRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

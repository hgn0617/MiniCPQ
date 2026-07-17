using MiniCPQ.Application.DTOs;

namespace MiniCPQ.Application.Interfaces;

public interface IExchangeRateService
{
    Task<IReadOnlyCollection<ExchangeRateDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<ExchangeRateDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<ExchangeRateDto> CreateAsync(CreateExchangeRateRequest request, CancellationToken cancellationToken);
    Task<ExchangeRateDto> UpdateAsync(Guid id, UpdateExchangeRateRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

using MiniCPQ.Application.DTOs;

namespace MiniCPQ.Application.Interfaces;

public interface IQuoteService
{
    Task<IReadOnlyCollection<QuoteDto>> GetAllAsync(string userId, bool isAdmin, CancellationToken cancellationToken);
    Task<QuoteDto> GetByIdAsync(Guid id, string userId, bool isAdmin, CancellationToken cancellationToken);
    Task<QuoteDto> CreateAsync(CreateQuoteRequest request, string userId, CancellationToken cancellationToken);
    Task<QuoteDto> AddItemAsync(Guid quoteId, AddQuoteItemRequest request, string userId, CancellationToken cancellationToken);
    Task<QuoteDto> UpdateItemAsync(Guid quoteId, Guid itemId, UpdateQuoteItemRequest request, string userId, CancellationToken cancellationToken);
    Task<QuoteDto> DeleteItemAsync(Guid quoteId, Guid itemId, string userId, CancellationToken cancellationToken);
    Task<QuoteDto> SubmitAsync(Guid quoteId, string userId, CancellationToken cancellationToken);
    Task<QuoteDto> ApproveAsync(Guid quoteId, ApproveQuoteRequest request, CancellationToken cancellationToken);
}

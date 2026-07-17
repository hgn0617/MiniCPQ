namespace MiniCPQ.Domain;

public sealed class Quote
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string CustomerName { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;
    public QuoteStatus Status { get; set; } = QuoteStatus.Draft;
    public decimal Cost { get; set; }
    public decimal? Price { get; set; }
    public required string CreatedByUserId { get; set; }

    public ICollection<QuoteItem> Items { get; set; } = [];
}

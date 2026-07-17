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

    public Guid? ExchangeRateId { get; set; }
    public ExchangeRate? ExchangeRate { get; set; }
    public required string CurrencyCode { get; set; } = "CNY";
    public required string CurrencyName { get; set; } = "人民币";
    public decimal CnyPerUnit { get; set; } = 1m;

    public ICollection<QuoteItem> Items { get; set; } = [];
}

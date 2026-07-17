namespace MiniCPQ.Domain;

public sealed class ExchangeRate
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string CurrencyCode { get; set; }
    public required string CurrencyName { get; set; }
    public decimal CnyPerUnit { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();

    public ICollection<Quote> Quotes { get; set; } = [];
}

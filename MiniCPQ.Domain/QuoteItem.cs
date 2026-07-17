namespace MiniCPQ.Domain;

public sealed class QuoteItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuoteId { get; set; }
    public Quote Quote { get; set; } = null!;
    public Guid? ServerId { get; set; }
    public Server? Server { get; set; }
    public int Quantity { get; set; }

    public string? ServerNameSnapshot { get; set; }
    public decimal? UnitCostSnapshot { get; set; }
    public decimal? TotalCostSnapshot { get; set; }

    public ICollection<QuoteMaterialSnapshot> MaterialSnapshots { get; set; } = [];
}

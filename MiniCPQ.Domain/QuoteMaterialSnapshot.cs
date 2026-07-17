namespace MiniCPQ.Domain;

public sealed class QuoteMaterialSnapshot
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid QuoteItemId { get; set; }
    public QuoteItem QuoteItem { get; set; } = null!;
    public Guid? MaterialId { get; set; }
    public Material? Material { get; set; }

    public required string MaterialName { get; set; }
    public required string MaterialType { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

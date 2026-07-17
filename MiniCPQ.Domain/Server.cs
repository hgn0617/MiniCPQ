namespace MiniCPQ.Domain;

public sealed class Server
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }

    public ICollection<ServerMaterial> Materials { get; set; } = [];
    public ICollection<QuoteItem> QuoteItems { get; set; } = [];
}

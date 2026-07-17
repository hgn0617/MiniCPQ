namespace MiniCPQ.Domain;

public sealed class ServerMaterial
{
    public Guid ServerId { get; set; }
    public Server Server { get; set; } = null!;
    public Guid MaterialId { get; set; }
    public Material Material { get; set; } = null!;
    public int Quantity { get; set; }
}

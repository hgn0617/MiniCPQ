namespace MiniCPQ.Domain;

public sealed class Material
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public required string Name { get; set; }
    public required string Type { get; set; }
    public decimal UnitPrice { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();

    public ICollection<ServerMaterial> ServerMaterials { get; set; } = [];
}

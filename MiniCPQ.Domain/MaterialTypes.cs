namespace MiniCPQ.Domain;

public static class MaterialTypes
{
    public const string Cpu = "CPU";
    public const string Memory = "RAM";
    public const string Storage = "ROM";
    public const string Motherboard = "MOTHERBOARD";
    public const string NetworkCard = "NIC";

    public static string GetServerGroup(string type) => type.Trim().ToUpperInvariant() switch
    {
        Cpu or "PROCESSOR" or "处理器" => Cpu,
        Memory or "MEMORY" or "内存" => Memory,
        Storage or "DISK" or "HDD" or "SSD" or "硬盘" => Storage,
        Motherboard or "MAINBOARD" or "BOARD" or "主板" => Motherboard,
        NetworkCard or "NETWORK" or "NETWORK CARD" or "网卡" => NetworkCard,
        var normalized => normalized
    };

    public static int? GetServerQuantityLimit(string type) => GetServerGroup(type) switch
    {
        Cpu => 1,
        Memory => 4,
        Storage => 3,
        Motherboard => 1,
        _ => null
    };

    public static string GetDisplayName(string type) => GetServerGroup(type) switch
    {
        Cpu => "CPU",
        Memory => "内存",
        Storage => "硬盘",
        Motherboard => "主板",
        NetworkCard => "网卡",
        _ => type.Trim().ToUpperInvariant()
    };
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MiniCPQ.Application.Common;
using MiniCPQ.Domain;
using MiniCPQ.Infrastructure.Identity;

namespace MiniCPQ.Infrastructure.Data;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var services = scope.ServiceProvider;
        var db = services.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync(cancellationToken);

        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in new[] { AppRoles.Admin, AppRoles.Sales })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                EnsureSucceeded(await roleManager.CreateAsync(new IdentityRole(role)), $"创建角色 {role}");
            }
        }

        await SeedCatalogAsync(db, cancellationToken);

        var configuration = services.GetRequiredService<IConfiguration>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("DatabaseInitializer");

        await SeedUserAsync(
            userManager,
            configuration["SeedUsers:AdminUserName"] ?? "admin",
            configuration["SeedUsers:AdminEmail"] ?? "admin@minicpq.local",
            configuration["SeedUsers:AdminPassword"],
            AppRoles.Admin,
            logger);
        await SeedUserAsync(
            userManager,
            configuration["SeedUsers:SalesUserName"] ?? "sales",
            configuration["SeedUsers:SalesEmail"] ?? "sales@minicpq.local",
            configuration["SeedUsers:SalesPassword"],
            AppRoles.Sales,
            logger);
    }

    private static async Task SeedCatalogAsync(ApplicationDbContext db, CancellationToken cancellationToken)
    {
        var materials = await db.Materials.ToListAsync(cancellationToken);
        var xeon = GetOrAddMaterial(materials, db, "Intel Xeon", MaterialTypes.Cpu, 2000m);
        _ = GetOrAddMaterial(materials, db, "Intel Core", MaterialTypes.Cpu, 1200m);
        var ram = GetOrAddMaterial(materials, db, "64GB RAM", MaterialTypes.Memory, 800m);
        var ssd2Tb = GetOrAddMaterial(materials, db, "2TB SSD", MaterialTypes.Storage, 1000m);
        var ssd1Tb = GetOrAddMaterial(materials, db, "1TB SSD", MaterialTypes.Storage, 600m);
        var motherboard = GetOrAddMaterial(materials, db, "Dell R750 主板", MaterialTypes.Motherboard, 1500m);
        var networkCard = GetOrAddMaterial(materials, db, "双口万兆网卡", MaterialTypes.NetworkCard, 600m);

        var server = await db.Servers
            .Include(x => x.Materials)
            .ThenInclude(x => x.Material)
            .SingleOrDefaultAsync(x => x.Name == "Dell R750", cancellationToken);
        if (server is null)
        {
            db.Servers.Add(new Server
            {
                Name = "Dell R750",
                Materials =
                [
                    new ServerMaterial { Material = xeon, Quantity = 1 },
                    new ServerMaterial { Material = ram, Quantity = 2 },
                    new ServerMaterial { Material = ssd2Tb, Quantity = 2 },
                    new ServerMaterial { Material = ssd1Tb, Quantity = 1 },
                    new ServerMaterial { Material = motherboard, Quantity = 1 },
                    new ServerMaterial { Material = networkCard, Quantity = 1 }
                ]
            });
        }
        else
        {
            NormalizeDefaultServerCpu(db, server);
        }

        if (!await db.ExchangeRates.AnyAsync(x => x.CurrencyCode == "USD", cancellationToken))
        {
            db.ExchangeRates.Add(new ExchangeRate
            {
                CurrencyCode = "USD",
                CurrencyName = "美元",
                CnyPerUnit = 7.20m
            });
        }

        if (!await db.ExchangeRates.AnyAsync(x => x.CurrencyCode == "EUR", cancellationToken))
        {
            db.ExchangeRates.Add(new ExchangeRate
            {
                CurrencyCode = "EUR",
                CurrencyName = "欧元",
                CnyPerUnit = 7.80m
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static Material GetOrAddMaterial(
        ICollection<Material> materials,
        ApplicationDbContext db,
        string name,
        string type,
        decimal unitPrice)
    {
        var existing = materials.SingleOrDefault(x => x.Name == name);
        if (existing is not null)
        {
            return existing;
        }

        var material = new Material { Name = name, Type = type, UnitPrice = unitPrice };
        materials.Add(material);
        db.Materials.Add(material);
        return material;
    }

    private static void NormalizeDefaultServerCpu(ApplicationDbContext db, Server server)
    {
        var cpuComponents = server.Materials
            .Where(x => MaterialTypes.GetServerGroup(x.Material.Type) == MaterialTypes.Cpu)
            .OrderByDescending(x => x.Material.Name == "Intel Xeon")
            .ThenBy(x => x.Material.Name)
            .ToList();
        if (cpuComponents.Count > 0)
        {
            cpuComponents[0].Quantity = 1;
            foreach (var extraCpu in cpuComponents.Skip(1))
            {
                db.ServerMaterials.Remove(extraCpu);
            }
        }

    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        string username,
        string email,
        string? password,
        string role,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning("未提供 {Role} 种子密码，跳过用户 {UserName}。", role, username);
            return;
        }

        var user = await userManager.FindByNameAsync(username) ?? await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser { UserName = username, Email = email, EmailConfirmed = true };
            EnsureSucceeded(await userManager.CreateAsync(user, password), $"创建用户 {username}");
        }
        else
        {
            if (!string.Equals(user.UserName, username, StringComparison.Ordinal))
            {
                EnsureSucceeded(await userManager.SetUserNameAsync(user, username), $"更新用户 {username} 的用户名");
            }

            if (!await userManager.CheckPasswordAsync(user, password))
            {
                var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
                EnsureSucceeded(await userManager.ResetPasswordAsync(user, resetToken, password), $"更新用户 {username} 的密码");
            }
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            EnsureSucceeded(await userManager.AddToRoleAsync(user, role), $"为用户 {username} 分配角色 {role}");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"{action}失败：{string.Join("; ", result.Errors.Select(x => x.Description))}");
        }
    }
}

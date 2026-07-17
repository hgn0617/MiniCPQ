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
        if (await db.Materials.AnyAsync(cancellationToken))
        {
            return;
        }

        var xeon = new Material { Name = "Intel Xeon", Type = "CPU", UnitPrice = 2000m };
        var core = new Material { Name = "Intel Core", Type = "CPU", UnitPrice = 1200m };
        var ram = new Material { Name = "64GB RAM", Type = "RAM", UnitPrice = 800m };
        var ssd2Tb = new Material { Name = "2TB SSD", Type = "ROM", UnitPrice = 1000m };
        var ssd1Tb = new Material { Name = "1TB SSD", Type = "ROM", UnitPrice = 600m };

        db.Materials.AddRange(xeon, core, ram, ssd2Tb, ssd1Tb);
        db.Servers.Add(new Server
        {
            Name = "Dell R750",
            Materials =
            [
                new ServerMaterial { Material = xeon, Quantity = 1 },
                new ServerMaterial { Material = core, Quantity = 1 },
                new ServerMaterial { Material = ram, Quantity = 2 },
                new ServerMaterial { Material = ssd2Tb, Quantity = 2 },
                new ServerMaterial { Material = ssd1Tb, Quantity = 1 }
            ]
        });
        await db.SaveChangesAsync(cancellationToken);
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

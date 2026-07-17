using MiniCPQ.Application.Common;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Domain;
using MiniCPQ.Infrastructure.Data;
using MiniCPQ.Infrastructure.Identity;
using MiniCPQ.Infrastructure.Services;

namespace MiniCPQ.Tests;

public sealed class BusinessFlowTests
{
    [Fact]
    public async Task ServerCost_IsCalculatedFromCurrentMaterialPrices()
    {
        await using var database = await TestDatabase.CreateAsync();
        var cpu = new Material { Name = "CPU-A", Type = "CPU", UnitPrice = 2000m };
        var ram = new Material { Name = "RAM-A", Type = "RAM", UnitPrice = 800m };
        var server = new Server
        {
            Name = "Server-A",
            Materials =
            [
                new ServerMaterial { Material = cpu, Quantity = 1 },
                new ServerMaterial { Material = ram, Quantity = 2 }
            ]
        };
        database.Context.Servers.Add(server);
        await database.Context.SaveChangesAsync();

        var service = new ServerService(database.Context);
        var before = await service.GetByIdAsync(server.Id, default);
        Assert.Equal(3600m, before.CurrentCost);

        ram.UnitPrice = 1000m;
        await database.Context.SaveChangesAsync();
        var after = await service.GetByIdAsync(server.Id, default);
        Assert.Equal(4000m, after.CurrentCost);
    }

    [Fact]
    public async Task SubmitQuote_CreatesSnapshots_AndFreezesHistoricalAmount()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = new ApplicationUser { Id = "sales-1", UserName = "sales@test.local", Email = "sales@test.local" };
        var cpu = new Material { Name = "CPU-B", Type = "CPU", UnitPrice = 2000m };
        var ram = new Material { Name = "RAM-B", Type = "RAM", UnitPrice = 800m };
        var server = new Server
        {
            Name = "Server-B",
            Materials =
            [
                new ServerMaterial { Material = cpu, Quantity = 1 },
                new ServerMaterial { Material = ram, Quantity = 2 }
            ]
        };
        database.Context.Users.Add(user);
        database.Context.Servers.Add(server);
        await database.Context.SaveChangesAsync();

        var service = new QuoteService(database.Context);
        var quote = await service.CreateAsync(new CreateQuoteRequest("ABC 公司"), user.Id, default);
        quote = await service.AddItemAsync(quote.Id, new AddQuoteItemRequest(server.Id, 2), user.Id, default);
        quote = await service.SubmitAsync(quote.Id, user.Id, default);

        Assert.Equal(QuoteStatus.Submitted, quote.Status);
        Assert.Equal(7200m, quote.Cost);
        Assert.Null(quote.Price);
        Assert.Equal(2, quote.Items.Single().Materials.Count);
        Assert.Equal(4, quote.Items.Single().Materials.Single(x => x.MaterialType == "RAM").Quantity);

        await Assert.ThrowsAsync<ValidationException>(() => service.ApproveAsync(
            quote.Id,
            new ApproveQuoteRequest(7000m),
            default));

        quote = await service.ApproveAsync(quote.Id, new ApproveQuoteRequest(9000m), default);
        Assert.Equal(QuoteStatus.Approved, quote.Status);
        Assert.Equal(9000m, quote.Price);
        Assert.Equal(1800m, quote.Profit);
        Assert.Equal(20m, quote.GrossMarginPercent);

        cpu.UnitPrice = 9999m;
        await database.Context.SaveChangesAsync();
        var historical = await service.GetByIdAsync(quote.Id, user.Id, false, default);
        Assert.Equal(7200m, historical.Cost);
        Assert.Equal(9000m, historical.Price);
        Assert.Equal(2000m, historical.Items.Single().Materials.Single(x => x.MaterialType == "CPU").UnitPrice);

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateItemAsync(
            quote.Id,
            quote.Items.Single().Id,
            new UpdateQuoteItemRequest(3),
            user.Id,
            default));
    }

    [Fact]
    public async Task MaterialUpdate_RejectsStaleVersion()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new MaterialService(database.Context);
        var created = await service.CreateAsync(new CreateMaterialRequest("GPU-A", "GPU", 3000m), default);

        var updated = await service.UpdateAsync(
            created.Id,
            new UpdateMaterialRequest(created.Name, created.Type, 3200m, created.Version),
            default);
        Assert.Equal(3200m, updated.UnitPrice);

        await Assert.ThrowsAsync<ConflictException>(() => service.UpdateAsync(
            created.Id,
            new UpdateMaterialRequest(created.Name, created.Type, 3500m, created.Version),
            default));
    }

}

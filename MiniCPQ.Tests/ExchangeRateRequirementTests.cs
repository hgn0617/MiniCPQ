using MiniCPQ.Application.Common;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Domain;
using MiniCPQ.Infrastructure.Identity;
using MiniCPQ.Infrastructure.Services;

namespace MiniCPQ.Tests;

public sealed class ExchangeRateRequirementTests
{
    [Fact]
    public async Task ExchangeRate_AdminCanMaintainRates_AndQuoteKeepsSnapshot()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = new ApplicationUser { Id = "sales-exchange", UserName = "sales-exchange", Email = "sales-exchange@test.local" };
        var cpu = new Material { Name = "CPU-Exchange", Type = MaterialTypes.Cpu, UnitPrice = 3600m };
        var server = new Server
        {
            Name = "Server-Exchange",
            Materials = [new ServerMaterial { Material = cpu, Quantity = 1 }]
        };
        database.Context.Users.Add(user);
        database.Context.Servers.Add(server);
        await database.Context.SaveChangesAsync();

        var exchangeRateService = new ExchangeRateService(database.Context);
        var usd = await exchangeRateService.CreateAsync(
            new CreateExchangeRateRequest("usd", "美元", 7.20m),
            default);
        Assert.Equal("USD", usd.CurrencyCode);

        var quoteService = new QuoteService(database.Context);
        var quote = await quoteService.CreateAsync(
            new CreateQuoteRequest("外币客户", usd.Id),
            user.Id,
            default);

        var updatedUsd = await exchangeRateService.UpdateAsync(
            usd.Id,
            new UpdateExchangeRateRequest("USD", "美元", 7.50m, usd.Version),
            default);
        Assert.Equal(7.50m, updatedUsd.CnyPerUnit);

        quote = await quoteService.AddItemAsync(
            quote.Id,
            new AddQuoteItemRequest(server.Id, 2),
            user.Id,
            default);
        quote = await quoteService.SubmitAsync(quote.Id, user.Id, default);
        quote = await quoteService.ApproveAsync(quote.Id, new ApproveQuoteRequest(9000m), default);

        Assert.Equal("USD", quote.CurrencyCode);
        Assert.Equal(7.20m, quote.CnyPerUnit);
        Assert.Equal(7200m, quote.Cost);
        Assert.Equal(1000m, quote.ForeignCost);
        Assert.Equal(9000m, quote.Price);
        Assert.Equal(1250m, quote.ForeignPrice);
        Assert.Equal(250m, quote.ForeignProfit);

        var nextQuote = await quoteService.CreateAsync(
            new CreateQuoteRequest("新汇率客户", usd.Id),
            user.Id,
            default);
        Assert.Equal(7.50m, nextQuote.CnyPerUnit);

        await Assert.ThrowsAsync<ConflictException>(() => exchangeRateService.UpdateAsync(
            usd.Id,
            new UpdateExchangeRateRequest("USD", "美元", 7.60m, usd.Version),
            default));
    }

    [Fact]
    public async Task ExchangeRate_RejectsCnyBecauseItIsTheBaseCurrency()
    {
        await using var database = await TestDatabase.CreateAsync();
        var service = new ExchangeRateService(database.Context);

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(
            new CreateExchangeRateRequest("CNY", "人民币", 1m),
            default));
    }
}

using System.Data;
using Microsoft.EntityFrameworkCore;
using MiniCPQ.Application.Common;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Application.Interfaces;
using MiniCPQ.Domain;
using MiniCPQ.Infrastructure.Data;

namespace MiniCPQ.Infrastructure.Services;

public sealed class QuoteService(ApplicationDbContext db) : IQuoteService
{
    public async Task<IReadOnlyCollection<QuoteDto>> GetAllAsync(
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var query = Query().AsNoTracking();
        if (!isAdmin)
        {
            query = query.Where(x => x.CreatedByUserId == userId);
        }

        var quotes = await query.OrderByDescending(x => x.CreatedAtUtc).ToListAsync(cancellationToken);
        return quotes.Select(Map).ToList();
    }

    public async Task<QuoteDto> GetByIdAsync(
        Guid id,
        string userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var quote = await Query().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("报价单不存在。");
        EnsureOwner(quote, userId, isAdmin);
        return Map(quote);
    }

    public async Task<QuoteDto> CreateAsync(
        CreateQuoteRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        var customerName = request.CustomerName.Trim();
        if (customerName.Length == 0)
        {
            throw new ValidationException("客户名称不能为空。");
        }

        var quote = new Quote { CustomerName = customerName, CreatedByUserId = userId };
        db.Quotes.Add(quote);
        await db.SaveChangesAsync(cancellationToken);
        return Map(quote);
    }

    public async Task<QuoteDto> AddItemAsync(
        Guid quoteId,
        AddQuoteItemRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        var quote = await db.Quotes.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == quoteId, cancellationToken)
            ?? throw new NotFoundException("报价单不存在。");
        EnsureOwner(quote, userId, false);
        EnsureDraft(quote);

        if (request.ServerId == Guid.Empty || request.Quantity <= 0)
        {
            throw new ValidationException("服务器编号必须有效，数量必须大于零。");
        }

        if (!await db.Servers.AnyAsync(x => x.Id == request.ServerId, cancellationToken))
        {
            throw new NotFoundException("服务器不存在。");
        }

        if (quote.Items.Any(x => x.ServerId == request.ServerId))
        {
            throw new ConflictException("该服务器已在报价单中，请修改已有条目的数量。");
        }

        db.QuoteItems.Add(new QuoteItem
        {
            QuoteId = quote.Id,
            ServerId = request.ServerId,
            Quantity = request.Quantity
        });
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(quoteId, userId, false, cancellationToken);
    }

    public async Task<QuoteDto> UpdateItemAsync(
        Guid quoteId,
        Guid itemId,
        UpdateQuoteItemRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        var quote = await db.Quotes.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == quoteId, cancellationToken)
            ?? throw new NotFoundException("报价单不存在。");
        EnsureOwner(quote, userId, false);
        EnsureDraft(quote);

        var item = quote.Items.SingleOrDefault(x => x.Id == itemId)
            ?? throw new NotFoundException("报价条目不存在。");
        if (request.Quantity <= 0)
        {
            throw new ValidationException("数量必须大于零。");
        }

        item.Quantity = request.Quantity;
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(quoteId, userId, false, cancellationToken);
    }

    public async Task<QuoteDto> DeleteItemAsync(
        Guid quoteId,
        Guid itemId,
        string userId,
        CancellationToken cancellationToken)
    {
        var quote = await db.Quotes.Include(x => x.Items).SingleOrDefaultAsync(x => x.Id == quoteId, cancellationToken)
            ?? throw new NotFoundException("报价单不存在。");
        EnsureOwner(quote, userId, false);
        EnsureDraft(quote);

        var item = quote.Items.SingleOrDefault(x => x.Id == itemId)
            ?? throw new NotFoundException("报价条目不存在。");
        db.QuoteItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(quoteId, userId, false, cancellationToken);
    }

    public async Task<QuoteDto> SubmitAsync(Guid quoteId, string userId, CancellationToken cancellationToken)
    {
        var isolationLevel = db.Database.IsNpgsql() ? IsolationLevel.RepeatableRead : IsolationLevel.Serializable;
        await using var transaction = await db.Database.BeginTransactionAsync(isolationLevel, cancellationToken);

        var quote = await Query().SingleOrDefaultAsync(x => x.Id == quoteId, cancellationToken)
            ?? throw new NotFoundException("报价单不存在。");
        EnsureOwner(quote, userId, false);
        EnsureDraft(quote);

        if (quote.Items.Count == 0)
        {
            throw new ValidationException("报价单至少需要一台服务器。");
        }

        decimal quoteCost = 0;
        foreach (var item in quote.Items)
        {
            var server = item.Server ?? throw new ConflictException("报价中的服务器已不存在，无法提交。");
            if (server.Materials.Count == 0)
            {
                throw new ConflictException($"服务器 {server.Name} 没有材料配置，无法提交。");
            }

            item.ServerNameSnapshot = server.Name;
            item.MaterialSnapshots.Clear();

            decimal unitCost = 0;
            foreach (var component in server.Materials)
            {
                int totalQuantity;
                try
                {
                    totalQuantity = checked(component.Quantity * item.Quantity);
                }
                catch (OverflowException)
                {
                    throw new ValidationException("服务器或材料数量过大。");
                }

                var lineTotal = component.Material.UnitPrice * totalQuantity;
                unitCost += component.Material.UnitPrice * component.Quantity;
                db.QuoteMaterialSnapshots.Add(new QuoteMaterialSnapshot
                {
                    QuoteItemId = item.Id,
                    MaterialId = component.MaterialId,
                    MaterialName = component.Material.Name,
                    MaterialType = component.Material.Type,
                    UnitPrice = component.Material.UnitPrice,
                    Quantity = totalQuantity,
                    TotalPrice = lineTotal
                });
            }

            item.UnitCostSnapshot = unitCost;
            item.TotalCostSnapshot = unitCost * item.Quantity;
            quoteCost += item.TotalCostSnapshot.Value;
        }

        quote.Cost = quoteCost;
        quote.Price = null;
        quote.Status = QuoteStatus.Submitted;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Map(quote);
    }

    public async Task<QuoteDto> ApproveAsync(
        Guid quoteId,
        ApproveQuoteRequest request,
        CancellationToken cancellationToken)
    {
        var quote = await Query().SingleOrDefaultAsync(x => x.Id == quoteId, cancellationToken)
            ?? throw new NotFoundException("报价单不存在。");
        if (quote.Status != QuoteStatus.Submitted)
        {
            throw new ConflictException("只有已提交的报价单可以审批。");
        }

        var price = decimal.Round(request.Price, 2, MidpointRounding.AwayFromZero);
        if (price <= 0)
        {
            throw new ValidationException("最终售价必须大于零。");
        }

        if (price < quote.Cost)
        {
            throw new ValidationException("最终售价不能低于已冻结的成本。");
        }

        quote.Price = price;
        quote.Status = QuoteStatus.Approved;
        await db.SaveChangesAsync(cancellationToken);
        return Map(quote);
    }

    private IQueryable<Quote> Query() => db.Quotes
        .AsSplitQuery()
        .Include(x => x.Items)
            .ThenInclude(x => x.Server)
                .ThenInclude(x => x!.Materials)
                    .ThenInclude(x => x.Material)
        .Include(x => x.Items)
            .ThenInclude(x => x.MaterialSnapshots);

    private static void EnsureOwner(Quote quote, string userId, bool isAdmin)
    {
        if (!isAdmin && quote.CreatedByUserId != userId)
        {
            throw new ForbiddenException("不能访问其他销售人员的报价单。");
        }
    }

    private static void EnsureDraft(Quote quote)
    {
        if (quote.Status != QuoteStatus.Draft)
        {
            throw new ConflictException("报价单提交后不能继续修改。");
        }
    }

    private static QuoteDto Map(Quote quote)
    {
        var items = quote.Items.OrderBy(x => x.Id).Select(item =>
        {
            var snapshots = item.MaterialSnapshots.OrderBy(x => x.MaterialType).ThenBy(x => x.MaterialName)
                .Select(x => new QuoteMaterialSnapshotDto(
                    x.Id,
                    x.MaterialId,
                    x.MaterialName,
                    x.MaterialType,
                    x.UnitPrice,
                    x.Quantity,
                    x.TotalPrice))
                .ToList();

            return new QuoteItemDto(
                item.Id,
                item.ServerId,
                item.ServerNameSnapshot ?? item.Server?.Name ?? "已删除的服务器",
                item.Quantity,
                item.UnitCostSnapshot,
                item.TotalCostSnapshot,
                snapshots);
        }).ToList();

        decimal? profit = quote.Price.HasValue ? quote.Price.Value - quote.Cost : null;
        decimal? grossMarginPercent = quote.Price > 0
            ? decimal.Round(profit!.Value / quote.Price.Value * 100, 2, MidpointRounding.AwayFromZero)
            : null;
        return new QuoteDto(
            quote.Id,
            quote.CustomerName,
            quote.CreatedAtUtc,
            quote.Status,
            quote.Cost,
            quote.Price,
            profit,
            grossMarginPercent,
            quote.CreatedByUserId,
            items);
    }
}

using Microsoft.EntityFrameworkCore;
using MiniCPQ.Application.Common;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Application.Interfaces;
using MiniCPQ.Domain;
using MiniCPQ.Infrastructure.Data;

namespace MiniCPQ.Infrastructure.Services;

public sealed class ExchangeRateService(ApplicationDbContext db) : IExchangeRateService
{
    public async Task<IReadOnlyCollection<ExchangeRateDto>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.ExchangeRates.AsNoTracking()
            .OrderBy(x => x.CurrencyCode)
            .Select(x => new ExchangeRateDto(x.Id, x.CurrencyCode, x.CurrencyName, x.CnyPerUnit, x.Version))
            .ToListAsync(cancellationToken);

    public async Task<ExchangeRateDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.ExchangeRates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("汇率不存在。");
        return Map(entity);
    }

    public async Task<ExchangeRateDto> CreateAsync(
        CreateExchangeRateRequest request,
        CancellationToken cancellationToken)
    {
        var entity = new ExchangeRate
        {
            CurrencyCode = NormalizeCode(request.CurrencyCode),
            CurrencyName = NormalizeName(request.CurrencyName),
            CnyPerUnit = NormalizeRate(request.CnyPerUnit)
        };
        db.ExchangeRates.Add(entity);
        await SaveAsync("币种代码已存在。", cancellationToken);
        return Map(entity);
    }

    public async Task<ExchangeRateDto> UpdateAsync(
        Guid id,
        UpdateExchangeRateRequest request,
        CancellationToken cancellationToken)
    {
        var entity = await db.ExchangeRates.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("汇率不存在。");
        if (request.Version == Guid.Empty || entity.Version != request.Version)
        {
            throw new ConflictException("汇率已被其他管理员修改，请刷新后重试。");
        }

        entity.CurrencyCode = NormalizeCode(request.CurrencyCode);
        entity.CurrencyName = NormalizeName(request.CurrencyName);
        entity.CnyPerUnit = NormalizeRate(request.CnyPerUnit);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("汇率已被其他管理员修改，请刷新后重试。");
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("币种代码已存在。");
        }

        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.ExchangeRates.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("汇率不存在。");
        db.ExchangeRates.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveAsync(string conflictMessage, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException(conflictMessage);
        }
    }

    private static ExchangeRateDto Map(ExchangeRate x) =>
        new(x.Id, x.CurrencyCode, x.CurrencyName, x.CnyPerUnit, x.Version);

    private static string NormalizeCode(string code)
    {
        var result = code.Trim().ToUpperInvariant();
        if (result.Length != 3 || result.Any(x => x is < 'A' or > 'Z'))
        {
            throw new ValidationException("币种代码必须是三个英文字母，例如 USD。");
        }

        if (result == "CNY")
        {
            throw new ValidationException("CNY 是系统本币，不需要在汇率表中维护。");
        }

        return result;
    }

    private static string NormalizeName(string name)
    {
        var result = name.Trim();
        return result.Length == 0 ? throw new ValidationException("币种名称不能为空。") : result;
    }

    private static decimal NormalizeRate(decimal rate) => rate <= 0
        ? throw new ValidationException("汇率必须大于零。")
        : decimal.Round(rate, 6, MidpointRounding.AwayFromZero);
}

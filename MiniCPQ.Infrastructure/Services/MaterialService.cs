using Microsoft.EntityFrameworkCore;
using MiniCPQ.Application.Common;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Application.Interfaces;
using MiniCPQ.Domain;
using MiniCPQ.Infrastructure.Data;

namespace MiniCPQ.Infrastructure.Services;

public sealed class MaterialService(ApplicationDbContext db) : IMaterialService
{
    public async Task<IReadOnlyCollection<MaterialDto>> GetAllAsync(CancellationToken cancellationToken) =>
        await db.Materials.AsNoTracking()
            .OrderBy(x => x.Type).ThenBy(x => x.Name)
            .Select(x => new MaterialDto(x.Id, x.Name, x.Type, x.UnitPrice, x.Version))
            .ToListAsync(cancellationToken);

    public async Task<MaterialDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Materials.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("材料不存在。");
        return Map(entity);
    }

    public async Task<MaterialDto> CreateAsync(CreateMaterialRequest request, CancellationToken cancellationToken)
    {
        var entity = new Material
        {
            Name = NormalizeRequired(request.Name, "材料名称"),
            Type = NormalizeType(request.Type),
            UnitPrice = request.UnitPrice
        };

        db.Materials.Add(entity);
        await SaveUniqueAsync("材料名称已存在。", cancellationToken);
        return Map(entity);
    }

    public async Task<MaterialDto> UpdateAsync(Guid id, UpdateMaterialRequest request, CancellationToken cancellationToken)
    {
        var entity = await db.Materials.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("材料不存在。");

        if (request.Version == Guid.Empty || entity.Version != request.Version)
        {
            throw new ConflictException("材料已被其他用户修改，请刷新后重试。");
        }

        entity.Name = NormalizeRequired(request.Name, "材料名称");
        entity.Type = NormalizeType(request.Type);
        entity.UnitPrice = request.UnitPrice;

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("材料已被其他用户修改，请刷新后重试。");
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("材料名称已存在。");
        }

        return Map(entity);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Materials.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("材料不存在。");

        if (await db.ServerMaterials.AnyAsync(x => x.MaterialId == id, cancellationToken))
        {
            throw new ConflictException("该材料仍被服务器配置使用，不能删除。");
        }

        db.Materials.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task SaveUniqueAsync(string message, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException(message);
        }
    }

    private static MaterialDto Map(Material x) => new(x.Id, x.Name, x.Type, x.UnitPrice, x.Version);

    private static string NormalizeRequired(string value, string field)
    {
        var result = value.Trim();
        return result.Length == 0 ? throw new ValidationException($"{field}不能为空。") : result;
    }

    private static string NormalizeType(string value) => NormalizeRequired(value, "材料类型").ToUpperInvariant();
}

using Microsoft.EntityFrameworkCore;
using MiniCPQ.Application.Common;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Application.Interfaces;
using MiniCPQ.Domain;
using MiniCPQ.Infrastructure.Data;

namespace MiniCPQ.Infrastructure.Services;

public sealed class ServerService(ApplicationDbContext db) : IServerService
{
    public async Task<IReadOnlyCollection<ServerDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var servers = await Query().AsNoTracking().OrderBy(x => x.Name).ToListAsync(cancellationToken);
        return servers.Select(Map).ToList();
    }

    public async Task<ServerDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await Query().AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("服务器不存在。");
        return Map(entity);
    }

    public async Task<ServerDto> CreateAsync(CreateServerRequest request, CancellationToken cancellationToken)
    {
        var inputs = await ValidateMaterialsAsync(request.Materials, cancellationToken);
        var entity = new Server { Name = NormalizeName(request.Name) };
        foreach (var input in inputs)
        {
            entity.Materials.Add(new ServerMaterial { MaterialId = input.MaterialId, Quantity = input.Quantity });
        }

        db.Servers.Add(entity);
        await SaveUniqueAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task<ServerDto> UpdateAsync(Guid id, UpdateServerRequest request, CancellationToken cancellationToken)
    {
        var inputs = await ValidateMaterialsAsync(request.Materials, cancellationToken);
        var entity = await db.Servers.Include(x => x.Materials).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("服务器不存在。");

        entity.Name = NormalizeName(request.Name);
        var requested = inputs.ToDictionary(x => x.MaterialId);

        foreach (var existing in entity.Materials.ToList())
        {
            if (requested.Remove(existing.MaterialId, out var input))
            {
                existing.Quantity = input.Quantity;
            }
            else
            {
                db.ServerMaterials.Remove(existing);
            }
        }

        foreach (var input in requested.Values)
        {
            db.ServerMaterials.Add(new ServerMaterial
            {
                ServerId = entity.Id,
                MaterialId = input.MaterialId,
                Quantity = input.Quantity
            });
        }

        await SaveUniqueAsync(cancellationToken);
        return await GetByIdAsync(entity.Id, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var entity = await db.Servers.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException("服务器不存在。");

        if (await db.QuoteItems.AnyAsync(x => x.ServerId == id, cancellationToken))
        {
            throw new ConflictException("该服务器已被报价单引用，不能删除；可修改配置或新增型号。");
        }

        db.Servers.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<Server> Query() => db.Servers
        .Include(x => x.Materials)
        .ThenInclude(x => x.Material);

    private async Task<IReadOnlyCollection<ServerMaterialInput>> ValidateMaterialsAsync(
        IReadOnlyCollection<ServerMaterialInput> inputs,
        CancellationToken cancellationToken)
    {
        if (inputs.Count == 0)
        {
            throw new ValidationException("服务器至少需要一种材料。");
        }

        if (inputs.Any(x => x.MaterialId == Guid.Empty || x.Quantity <= 0))
        {
            throw new ValidationException("材料编号必须有效，数量必须大于零。");
        }

        if (inputs.Select(x => x.MaterialId).Distinct().Count() != inputs.Count)
        {
            throw new ValidationException("同一种材料不能重复配置，请使用数量字段。");
        }

        var ids = inputs.Select(x => x.MaterialId).ToArray();
        var existingCount = await db.Materials.CountAsync(x => ids.Contains(x.Id), cancellationToken);
        if (existingCount != ids.Length)
        {
            throw new ValidationException("服务器配置中包含不存在的材料。");
        }

        return inputs;
    }

    private async Task SaveUniqueAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            throw new ConflictException("服务器名称已存在，或配置与现有数据冲突。");
        }
    }

    private static ServerDto Map(Server server)
    {
        var materials = server.Materials
            .OrderBy(x => x.Material.Type).ThenBy(x => x.Material.Name)
            .Select(x => new ServerMaterialDto(
                x.MaterialId,
                x.Material.Name,
                x.Material.Type,
                x.Material.UnitPrice,
                x.Quantity,
                x.Material.UnitPrice * x.Quantity))
            .ToList();
        return new ServerDto(server.Id, server.Name, materials.Sum(x => x.Subtotal), materials);
    }

    private static string NormalizeName(string name)
    {
        var result = name.Trim();
        return result.Length == 0 ? throw new ValidationException("服务器名称不能为空。") : result;
    }
}

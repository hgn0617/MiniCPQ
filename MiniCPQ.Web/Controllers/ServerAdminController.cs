using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCPQ.Application.Common;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Application.Interfaces;
using MiniCPQ.Web.Models;

namespace MiniCPQ.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[Route("admin/servers")]
public sealed class ServerAdminController(
    IServerService serverService,
    IMaterialService materialService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await serverService.GetAllAsync(cancellationToken));

    [HttpGet("create")]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View(await BuildFormAsync(null, null, cancellationToken));

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServerFormViewModel model, CancellationToken cancellationToken)
    {
        var request = BuildRequest(model);
        if (!ModelState.IsValid || request is null)
        {
            return View(await BuildFormAsync(null, model, cancellationToken));
        }

        try
        {
            await serverService.CreateAsync(request, cancellationToken);
            TempData["Success"] = "服务器配置已创建。";
            return RedirectToAction(nameof(Index));
        }
        catch (AppException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(await BuildFormAsync(null, model, cancellationToken));
        }
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken) =>
        View(await BuildFormAsync(id, null, cancellationToken));

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, ServerFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var request = BuildRequest(model);
        if (!ModelState.IsValid || request is null)
        {
            return View(await BuildFormAsync(id, model, cancellationToken));
        }

        try
        {
            await serverService.UpdateAsync(id, new UpdateServerRequest(request.Name, request.Materials), cancellationToken);
            TempData["Success"] = "服务器配置已更新。";
            return RedirectToAction(nameof(Index));
        }
        catch (AppException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(await BuildFormAsync(id, model, cancellationToken));
        }
    }

    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await serverService.DeleteAsync(id, cancellationToken);
            TempData["Success"] = "服务器配置已删除。";
        }
        catch (AppException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }

    private CreateServerRequest? BuildRequest(ServerFormViewModel model)
    {
        var selected = model.Materials.Where(x => x.Selected).ToList();
        if (selected.Count == 0)
        {
            ModelState.AddModelError(nameof(model.Materials), "请至少选择一种材料。");
            return null;
        }

        if (selected.Any(x => x.Quantity <= 0))
        {
            ModelState.AddModelError(nameof(model.Materials), "已选材料的数量必须大于零。");
            return null;
        }

        return new CreateServerRequest(
            model.Name,
            selected.Select(x => new ServerMaterialInput(x.MaterialId, x.Quantity)).ToList());
    }

    private async Task<ServerFormViewModel> BuildFormAsync(
        Guid? serverId,
        ServerFormViewModel? posted,
        CancellationToken cancellationToken)
    {
        var materials = await materialService.GetAllAsync(cancellationToken);
        ServerDto? server = serverId.HasValue
            ? await serverService.GetByIdAsync(serverId.Value, cancellationToken)
            : null;
        var postedById = posted?.Materials.ToDictionary(x => x.MaterialId);
        var configuredById = server?.Materials.ToDictionary(x => x.MaterialId);

        return new ServerFormViewModel
        {
            Id = serverId ?? Guid.Empty,
            Name = posted?.Name ?? server?.Name ?? string.Empty,
            Materials = materials.Select(material =>
            {
                ServerMaterialOptionViewModel? postedOption = null;
                ServerMaterialDto? configured = null;
                postedById?.TryGetValue(material.Id, out postedOption);
                configuredById?.TryGetValue(material.Id, out configured);
                return new ServerMaterialOptionViewModel
                {
                    MaterialId = material.Id,
                    Name = material.Name,
                    Type = material.Type,
                    UnitPrice = material.UnitPrice,
                    Selected = postedOption?.Selected ?? configured is not null,
                    Quantity = postedOption?.Quantity ?? configured?.Quantity ?? 1
                };
            }).ToList()
        };
    }
}

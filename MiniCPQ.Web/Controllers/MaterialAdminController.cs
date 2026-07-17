using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCPQ.Application.Common;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Application.Interfaces;
using MiniCPQ.Web.Models;

namespace MiniCPQ.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[Route("admin/materials")]
public sealed class MaterialAdminController(IMaterialService service) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await service.GetAllAsync(cancellationToken));

    [HttpGet("create")]
    public IActionResult Create() => View(new MaterialFormViewModel());

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaterialFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await service.CreateAsync(new CreateMaterialRequest(model.Name, model.Type, model.UnitPrice), cancellationToken);
            TempData["Success"] = "材料已创建。";
            return RedirectToAction(nameof(Index));
        }
        catch (AppException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        var material = await service.GetByIdAsync(id, cancellationToken);
        return View(new MaterialFormViewModel
        {
            Id = material.Id,
            Name = material.Name,
            Type = material.Type,
            UnitPrice = material.UnitPrice,
            Version = material.Version
        });
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, MaterialFormViewModel model, CancellationToken cancellationToken)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await service.UpdateAsync(
                id,
                new UpdateMaterialRequest(model.Name, model.Type, model.UnitPrice, model.Version),
                cancellationToken);
            TempData["Success"] = "材料已更新。";
            return RedirectToAction(nameof(Index));
        }
        catch (AppException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await service.DeleteAsync(id, cancellationToken);
            TempData["Success"] = "材料已删除。";
        }
        catch (AppException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCPQ.Application.Common;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Application.Interfaces;
using MiniCPQ.Web.Models;

namespace MiniCPQ.Web.Controllers;

[Authorize(Roles = AppRoles.Admin)]
[Route("admin/exchange-rates")]
public sealed class ExchangeRateAdminController(IExchangeRateService service) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await service.GetAllAsync(cancellationToken));

    [HttpGet("create")]
    public IActionResult Create() => View(new ExchangeRateFormViewModel());

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ExchangeRateFormViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            await service.CreateAsync(
                new CreateExchangeRateRequest(model.CurrencyCode, model.CurrencyName, model.CnyPerUnit),
                cancellationToken);
            TempData["Success"] = "汇率已创建。";
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
        var rate = await service.GetByIdAsync(id, cancellationToken);
        return View(new ExchangeRateFormViewModel
        {
            Id = rate.Id,
            CurrencyCode = rate.CurrencyCode,
            CurrencyName = rate.CurrencyName,
            CnyPerUnit = rate.CnyPerUnit,
            Version = rate.Version
        });
    }

    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        ExchangeRateFormViewModel model,
        CancellationToken cancellationToken)
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
                new UpdateExchangeRateRequest(
                    model.CurrencyCode,
                    model.CurrencyName,
                    model.CnyPerUnit,
                    model.Version),
                cancellationToken);
            TempData["Success"] = "汇率已更新；已有报价继续使用原汇率快照。";
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
            TempData["Success"] = "汇率已删除；历史报价的汇率快照不会受到影响。";
        }
        catch (AppException exception)
        {
            TempData["Error"] = exception.Message;
        }

        return RedirectToAction(nameof(Index));
    }
}

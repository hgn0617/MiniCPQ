using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCPQ.Application.Common;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Application.Interfaces;
using MiniCPQ.Domain;
using MiniCPQ.Web.Models;

namespace MiniCPQ.Web.Controllers;

[Authorize(Roles = AppRoles.AdminOrSales)]
[Route("quotes")]
public sealed class QuotePortalController(
    IQuoteService quoteService,
    IServerService serverService) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(await quoteService.GetAllAsync(UserId, IsAdmin, cancellationToken));

    [Authorize(Roles = AppRoles.Sales)]
    [HttpGet("create")]
    public IActionResult Create() => View(new QuoteCreateViewModel());

    [Authorize(Roles = AppRoles.Sales)]
    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QuoteCreateViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var quote = await quoteService.CreateAsync(new CreateQuoteRequest(model.CustomerName), UserId, cancellationToken);
            TempData["Success"] = "报价单已创建，请添加服务器。";
            return RedirectToAction(nameof(Details), new { id = quote.Id });
        }
        catch (AppException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View(model);
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        var quote = await quoteService.GetByIdAsync(id, UserId, IsAdmin, cancellationToken);
        var servers = quote.Status == QuoteStatus.Draft && User.IsInRole(AppRoles.Sales)
            ? await serverService.GetAllAsync(cancellationToken)
            : [];
        return View(new QuoteDetailsViewModel { Quote = quote, AvailableServers = servers });
    }

    [Authorize(Roles = AppRoles.Sales)]
    [HttpPost("{id:guid}/items")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddItem(Guid id, Guid serverId, int quantity, CancellationToken cancellationToken)
    {
        await RunActionAsync(
            () => quoteService.AddItemAsync(id, new AddQuoteItemRequest(serverId, quantity), UserId, cancellationToken),
            "服务器已添加。");
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = AppRoles.Sales)]
    [HttpPost("{id:guid}/items/{itemId:guid}/update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateItem(
        Guid id,
        Guid itemId,
        int quantity,
        CancellationToken cancellationToken)
    {
        await RunActionAsync(
            () => quoteService.UpdateItemAsync(id, itemId, new UpdateQuoteItemRequest(quantity), UserId, cancellationToken),
            "数量已更新。");
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = AppRoles.Sales)]
    [HttpPost("{id:guid}/items/{itemId:guid}/delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteItem(Guid id, Guid itemId, CancellationToken cancellationToken)
    {
        await RunActionAsync(
            () => quoteService.DeleteItemAsync(id, itemId, UserId, cancellationToken),
            "服务器已移除。");
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = AppRoles.Sales)]
    [HttpPost("{id:guid}/submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(Guid id, CancellationToken cancellationToken)
    {
        await RunActionAsync(
            () => quoteService.SubmitAsync(id, UserId, cancellationToken),
            "报价已提交，成本和材料价格快照已冻结，等待管理员定价。");
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = AppRoles.Admin)]
    [HttpPost("{id:guid}/approve")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id, decimal price, CancellationToken cancellationToken)
    {
        await RunActionAsync(
            () => quoteService.ApproveAsync(id, new ApproveQuoteRequest(price), cancellationToken),
            "最终售价已确认，报价审批完成。");
        return RedirectToAction(nameof(Details), new { id });
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new InvalidOperationException("当前登录用户缺少用户编号。");

    private bool IsAdmin => User.IsInRole(AppRoles.Admin);

    private async Task RunActionAsync(Func<Task<QuoteDto>> action, string successMessage)
    {
        try
        {
            await action();
            TempData["Success"] = successMessage;
        }
        catch (AppException exception)
        {
            TempData["Error"] = exception.Message;
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MiniCPQ.Application.Common;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Application.Interfaces;
using MiniCPQ.Infrastructure.Identity;

namespace MiniCPQ.Web.Controllers;

[ApiController]
[Route("api/quotes")]
[Authorize(Roles = AppRoles.AdminOrSales)]
public sealed class QuotesController(
    IQuoteService service,
    UserManager<ApplicationUser> userManager) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<QuoteDto>> GetAll(CancellationToken cancellationToken) =>
        service.GetAllAsync(GetUserId(), User.IsInRole(AppRoles.Admin), cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<QuoteDto> GetById(Guid id, CancellationToken cancellationToken) =>
        service.GetByIdAsync(id, GetUserId(), User.IsInRole(AppRoles.Admin), cancellationToken);

    [HttpPost]
    [Authorize(Roles = AppRoles.Sales)]
    [ProducesResponseType<QuoteDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<QuoteDto>> Create(CreateQuoteRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, GetUserId(), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPost("{id:guid}/items")]
    [Authorize(Roles = AppRoles.Sales)]
    public Task<QuoteDto> AddItem(Guid id, AddQuoteItemRequest request, CancellationToken cancellationToken) =>
        service.AddItemAsync(id, request, GetUserId(), cancellationToken);

    [HttpPut("{id:guid}/items/{itemId:guid}")]
    [Authorize(Roles = AppRoles.Sales)]
    public Task<QuoteDto> UpdateItem(
        Guid id,
        Guid itemId,
        UpdateQuoteItemRequest request,
        CancellationToken cancellationToken) =>
        service.UpdateItemAsync(id, itemId, request, GetUserId(), cancellationToken);

    [HttpDelete("{id:guid}/items/{itemId:guid}")]
    [Authorize(Roles = AppRoles.Sales)]
    public Task<QuoteDto> DeleteItem(Guid id, Guid itemId, CancellationToken cancellationToken) =>
        service.DeleteItemAsync(id, itemId, GetUserId(), cancellationToken);

    [HttpPost("{id:guid}/submit")]
    [Authorize(Roles = AppRoles.Sales)]
    public Task<QuoteDto> Submit(Guid id, CancellationToken cancellationToken) =>
        service.SubmitAsync(id, GetUserId(), cancellationToken);

    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = AppRoles.Admin)]
    public Task<QuoteDto> Approve(
        Guid id,
        ApproveQuoteRequest request,
        CancellationToken cancellationToken) =>
        service.ApproveAsync(id, request, cancellationToken);

    private string GetUserId() => userManager.GetUserId(User)
        ?? throw new InvalidOperationException("当前登录用户缺少用户编号。");
}

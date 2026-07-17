using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCPQ.Application.Common;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Application.Interfaces;

namespace MiniCPQ.Web.Controllers;

[ApiController]
[Route("api/exchange-rates")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class ExchangeRatesController(IExchangeRateService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<ExchangeRateDto>> GetAll(CancellationToken cancellationToken) =>
        service.GetAllAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<ExchangeRateDto> GetById(Guid id, CancellationToken cancellationToken) =>
        service.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    [ProducesResponseType<ExchangeRateDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ExchangeRateDto>> Create(
        CreateExchangeRateRequest request,
        CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public Task<ExchangeRateDto> Update(
        Guid id,
        UpdateExchangeRateRequest request,
        CancellationToken cancellationToken) =>
        service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

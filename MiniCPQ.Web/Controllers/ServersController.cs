using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCPQ.Application.Common;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Application.Interfaces;

namespace MiniCPQ.Web.Controllers;

[ApiController]
[Route("api/servers")]
[Authorize(Roles = AppRoles.AdminOrSales)]
public sealed class ServersController(IServerService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<ServerDto>> GetAll(CancellationToken cancellationToken) =>
        service.GetAllAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<ServerDto> GetById(Guid id, CancellationToken cancellationToken) =>
        service.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType<ServerDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ServerDto>> Create(CreateServerRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    public Task<ServerDto> Update(Guid id, UpdateServerRequest request, CancellationToken cancellationToken) =>
        service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

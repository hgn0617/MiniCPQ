using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCPQ.Application.Common;
using MiniCPQ.Application.DTOs;
using MiniCPQ.Application.Interfaces;

namespace MiniCPQ.Web.Controllers;

[ApiController]
[Route("api/materials")]
[Authorize(Roles = AppRoles.Admin)]
public sealed class MaterialsController(IMaterialService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<MaterialDto>> GetAll(CancellationToken cancellationToken) =>
        service.GetAllAsync(cancellationToken);

    [HttpGet("{id:guid}")]
    public Task<MaterialDto> GetById(Guid id, CancellationToken cancellationToken) =>
        service.GetByIdAsync(id, cancellationToken);

    [HttpPost]
    [ProducesResponseType<MaterialDto>(StatusCodes.Status201Created)]
    public async Task<ActionResult<MaterialDto>> Create(CreateMaterialRequest request, CancellationToken cancellationToken)
    {
        var result = await service.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public Task<MaterialDto> Update(Guid id, UpdateMaterialRequest request, CancellationToken cancellationToken) =>
        service.UpdateAsync(id, request, cancellationToken);

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}

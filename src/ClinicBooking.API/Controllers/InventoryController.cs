using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Inventory.Commands.AdjustInventoryStock;
using ClinicBooking.Application.Features.Inventory.Commands.CreateInventoryItem;
using ClinicBooking.Application.Features.Inventory.Commands.DeleteInventoryItem;
using ClinicBooking.Application.Features.Inventory.DTOs;
using ClinicBooking.Application.Features.Inventory.Queries.GetClinicInventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Authorize(Roles = "Admin,ClinicAdmin,Staff,Accountant,Nurse")]
public sealed class InventoryController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService _currentUserService;

    public InventoryController(ITenantContext tenantContext, ICurrentUserService currentUserService)
    {
        _tenantContext = tenantContext;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<MedicalInventoryItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MedicalInventoryItemDto>>> GetInventory(
        [FromQuery] Guid? clinicId,
        CancellationToken cancellationToken)
    {
        var effectiveClinicId = _currentUserService.Role == "ClinicAdmin" && _currentUserService.ClinicId.HasValue
            ? _currentUserService.ClinicId
            : clinicId;

        var result = await Mediator.Send(new GetClinicInventoryQuery(effectiveClinicId), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ClinicAdmin")]
    [ProducesResponseType(typeof(MedicalInventoryItemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MedicalInventoryItemDto>> CreateItem(
        [FromBody] CreateInventoryItemCommand command,
        CancellationToken cancellationToken)
    {
        var finalCommand = command with { TenantId = _tenantContext.TenantId };
        var result = await Mediator.Send(finalCommand, cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{id:guid}/adjust")]
    [ProducesResponseType(typeof(MedicalInventoryItemDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<MedicalInventoryItemDto>> AdjustStock(
        [FromRoute] Guid id,
        [FromBody] AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new AdjustInventoryStockCommand(id, request.DeltaQuantity), cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,ClinicAdmin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteItem(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteInventoryItemCommand(id), cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleResult(result);
        }
        return NoContent();
    }
}

public sealed record AdjustStockRequest(int DeltaQuantity);

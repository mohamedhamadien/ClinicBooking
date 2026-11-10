using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.MedicalAttachments.Commands.DeleteMedicalAttachment;
using ClinicBooking.Application.Features.MedicalAttachments.Commands.UploadMedicalAttachment;
using ClinicBooking.Application.Features.MedicalAttachments.DTOs;
using ClinicBooking.Application.Features.MedicalAttachments.Queries.GetPatientAttachments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Authorize]
public sealed class AttachmentsController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;

    public AttachmentsController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    [HttpGet("patient/{patientId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<MedicalAttachmentDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<MedicalAttachmentDto>>> GetPatientAttachments(
        [FromRoute] Guid patientId,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetPatientAttachmentsQuery(patientId), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ClinicAdmin,Doctor,Nurse,Staff,Receptionist")]
    [ProducesResponseType(typeof(MedicalAttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<MedicalAttachmentDto>> UploadAttachment(
        [FromBody] UploadMedicalAttachmentCommand command,
        CancellationToken cancellationToken)
    {
        var finalCommand = command with { TenantId = _tenantContext.TenantId };
        var result = await Mediator.Send(finalCommand, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,ClinicAdmin,Doctor")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAttachment(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteMedicalAttachmentCommand(id), cancellationToken);
        if (!result.IsSuccess)
        {
            return HandleResult(result);
        }
        return NoContent();
    }
}

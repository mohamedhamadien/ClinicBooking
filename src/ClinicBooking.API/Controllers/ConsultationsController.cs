using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Consultations.Commands.CreateConsultation;
using ClinicBooking.Application.Features.Consultations.Commands.UpdatePatientMedicalHistory;
using ClinicBooking.Application.Features.Consultations.DTOs;
using ClinicBooking.Application.Features.Consultations.Queries.GetPatientMedicalRecord;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Authorize(Roles = "Doctor,Admin,ClinicAdmin")]
public sealed class ConsultationsController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;

    public ConsultationsController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Record clinical consultation notes, diagnosis, and optional vitals (Doctor, Admin, ClinicAdmin).
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ConsultationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ConsultationDto>> CreateConsultation(
        [FromBody] CreateConsultationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieve full clinical EMR chart (past consultations, prescriptions, vitals timeline, allergies).
    /// </summary>
    [HttpGet("patients/{patientId:guid}")]
    [ProducesResponseType(typeof(PatientMedicalRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientMedicalRecordDto>> GetPatientMedicalRecord(
        [FromRoute] Guid patientId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var query = new GetPatientMedicalRecordQuery(patientId, tenantId);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Update patient medical background: blood group, allergies, and chronic conditions.
    /// </summary>
    [HttpPut("patients/{patientId:guid}/medical-history")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateMedicalHistory(
        [FromRoute] Guid patientId,
        [FromBody] UpdateMedicalHistoryRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePatientMedicalHistoryCommand(
            patientId,
            request.BloodGroup,
            request.Allergies,
            request.ChronicConditions,
            request.MedicalNotes);

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}

public sealed record UpdateMedicalHistoryRequest(
    string? BloodGroup,
    string? Allergies,
    string? ChronicConditions,
    string? MedicalNotes);

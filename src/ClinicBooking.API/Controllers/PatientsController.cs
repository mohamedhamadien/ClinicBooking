using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Consultations.DTOs;
using ClinicBooking.Application.Features.Consultations.Queries.GetPatientMedicalRecord;
using ClinicBooking.Application.Features.Patients.Commands.CreatePatient;
using ClinicBooking.Application.Features.Patients.DTOs;
using ClinicBooking.Application.Features.Patients.Queries.GetPatients;
using ClinicBooking.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Authorize(Roles = "Doctor,Admin,ClinicAdmin,Staff,Receptionist,Nurse")]
public sealed class PatientsController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;

    public PatientsController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Retrieve all registered patients with optional search term (Name, Phone, Email, Blood Group).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PatientSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PatientSummaryDto>>> GetPatients(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var query = new GetPatientsQuery(tenantId, search);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Register a new patient account and medical record at reception / clinic desk.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(PatientSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PatientSummaryDto>> CreatePatient(
        [FromBody] CreatePatientApiRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;

        var command = new CreatePatientCommand(
            tenantId,
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.DateOfBirth,
            request.Gender,
            request.BloodGroup,
            request.Allergies,
            request.ChronicConditions,
            request.EmergencyContact,
            request.Password);

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    /// <summary>
    /// Retrieve patient complete clinical EMR record and history.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PatientMedicalRecordDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientMedicalRecordDto>> GetPatientById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var query = new GetPatientMedicalRecordQuery(id, tenantId);
        var result = await Mediator.Send(query, cancellationToken);
        return HandleResult(result);
    }
}

public sealed record CreatePatientApiRequest(
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    DateOnly DateOfBirth,
    Gender Gender,
    string? BloodGroup = null,
    string? Allergies = null,
    string? ChronicConditions = null,
    string? EmergencyContact = null,
    string? Password = null);

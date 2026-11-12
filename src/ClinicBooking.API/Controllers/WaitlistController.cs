using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Appointments.Commands.BookAppointment;
using ClinicBooking.Application.Features.Appointments.DTOs;
using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Route("api/v1/waitlist")]
[Authorize]
public sealed class WaitlistController : ApiControllerBase
{
    private readonly IWaitlistRepository _waitlistRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ICurrentUserService _currentUserService;

    public WaitlistController(
        IWaitlistRepository waitlistRepository,
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        IClinicRepository clinicRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService currentUserService)
    {
        _waitlistRepository = waitlistRepository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _clinicRepository = clinicRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    private Guid? EnforceClinicScope(Guid? requestedClinicId)
    {
        if (_currentUserService.Role is not "Admin" && _currentUserService.ClinicId.HasValue && _currentUserService.ClinicId.Value != Guid.Empty)
        {
            return _currentUserService.ClinicId.Value;
        }
        return requestedClinicId;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WaitlistEntryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<WaitlistEntryDto>>> GetWaitlist(
        [FromQuery] Guid? clinicId,
        [FromQuery] Guid? doctorId,
        [FromQuery] DateOnly? preferredDate,
        [FromQuery] WaitlistStatus? status,
        CancellationToken cancellationToken)
    {
        var effectiveClinicId = EnforceClinicScope(clinicId);
        var entries = await _waitlistRepository.ListAsync(effectiveClinicId, doctorId, preferredDate, status, cancellationToken);

        var dtos = new List<WaitlistEntryDto>(entries.Count);
        foreach (var entry in entries)
        {
            dtos.Add(await MapToDtoAsync(entry, cancellationToken));
        }

        return Ok(dtos);
    }

    [HttpPost]
    [ProducesResponseType(typeof(WaitlistEntryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<WaitlistEntryDto>> AddToWaitlist(
        [FromBody] CreateWaitlistEntryRequest request,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? request.TenantId;
        var effectiveClinicId = EnforceClinicScope(request.ClinicId) ?? request.ClinicId;

        try
        {
            var entry = WaitlistEntry.Create(
                tenantId,
                effectiveClinicId,
                request.DoctorId,
                request.PatientId,
                request.PreferredDate,
                request.PreferredStartTime,
                request.PreferredEndTime,
                request.Priority,
                request.Notes);

            await _waitlistRepository.AddAsync(entry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(await MapToDtoAsync(entry, cancellationToken));
        }
        catch (DomainValidationException ex)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Waitlist Validation Failed",
                Detail = ex.Message
            });
        }
    }

    [HttpPost("{id:guid}/offer")]
    [Authorize(Roles = "Admin,ClinicAdmin,Receptionist,Doctor,Staff")]
    [ProducesResponseType(typeof(WaitlistEntryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WaitlistEntryDto>> OfferSlot(
        [FromRoute] Guid id,
        [FromBody] OfferWaitlistSlotRequest request,
        CancellationToken cancellationToken)
    {
        var entry = await _waitlistRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null) return NotFound();

        try
        {
            var expiresAt = request.OfferExpiresAtUtc ?? _dateTimeProvider.UtcNow.AddMinutes(30);
            entry.OfferSlot(request.SlotStartUtc, request.SlotEndUtc, expiresAt);
            await _waitlistRepository.UpdateAsync(entry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Ok(await MapToDtoAsync(entry, cancellationToken));
        }
        catch (DomainValidationException ex)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Invalid Waitlist State",
                Detail = ex.Message
            });
        }
    }

    [HttpPost("{id:guid}/claim")]
    [ProducesResponseType(typeof(WaitlistClaimResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WaitlistClaimResultDto>> ClaimOfferedSlot(
        [FromRoute] Guid id,
        [FromBody] ClaimWaitlistSlotRequest? request,
        CancellationToken cancellationToken)
    {
        var entry = await _waitlistRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null) return NotFound();

        var startUtc = request?.StartTimeUtc ?? entry.OfferedSlotStartUtc;
        var endUtc = request?.EndTimeUtc ?? entry.OfferedSlotEndUtc;

        if (!startUtc.HasValue || !endUtc.HasValue)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Missing Slot Range",
                Detail = "No offered slot times found on this waitlist entry and none were provided in the request."
            });
        }

        if (entry.OfferExpiresAtUtc.HasValue && _dateTimeProvider.UtcNow > entry.OfferExpiresAtUtc.Value)
        {
            entry.ExpireOffer();
            await _waitlistRepository.UpdateAsync(entry, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Waitlist Offer Expired",
                Detail = "The offered slot has expired."
            });
        }

        var bookCommand = new BookAppointmentCommand(
            TenantId: entry.TenantId,
            DoctorId: entry.DoctorId,
            PatientId: entry.PatientId,
            ClinicId: entry.ClinicId,
            StartTimeUtc: startUtc.Value,
            EndTimeUtc: endUtc.Value,
            Notes: entry.Notes ?? "Booked from Waitlist recovery",
            Source: AppointmentSource.Receptionist,
            CreatedByStaffId: CurrentUserId);

        var bookResult = await Mediator.Send(bookCommand, cancellationToken);
        if (bookResult.IsFailure)
        {
            return HandleResult(bookResult);
        }

        entry.MarkBooked(bookResult.Value.Id);
        await _waitlistRepository.UpdateAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var entryDto = await MapToDtoAsync(entry, cancellationToken);
        return Ok(new WaitlistClaimResultDto(entryDto, bookResult.Value));
    }

    [HttpPost("{id:guid}/expire")]
    [Authorize(Roles = "Admin,ClinicAdmin,Receptionist,Doctor,Staff")]
    [ProducesResponseType(typeof(WaitlistEntryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WaitlistEntryDto>> ExpireAndCascadeOffer(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var entry = await _waitlistRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null) return NotFound();

        var slotStart = entry.OfferedSlotStartUtc;
        var slotEnd = entry.OfferedSlotEndUtc;

        entry.ExpireOffer();
        await _waitlistRepository.UpdateAsync(entry, cancellationToken);

        // Cascade offer to the next waiting candidate if slot times were present
        if (slotStart.HasValue && slotEnd.HasValue)
        {
            var nextCandidate = await _waitlistRepository.GetNextWaitingCandidateAsync(
                entry.TenantId,
                entry.ClinicId,
                entry.DoctorId,
                entry.PreferredDate,
                TimeOnly.FromDateTime(slotStart.Value),
                cancellationToken);

            if (nextCandidate is not null && nextCandidate.Id != entry.Id)
            {
                nextCandidate.OfferSlot(slotStart.Value, slotEnd.Value, _dateTimeProvider.UtcNow.AddMinutes(30));
                await _waitlistRepository.UpdateAsync(nextCandidate, cancellationToken);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Ok(await MapToDtoAsync(entry, cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(WaitlistEntryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<WaitlistEntryDto>> Cancel(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var entry = await _waitlistRepository.GetByIdAsync(id, cancellationToken);
        if (entry is null) return NotFound();

        entry.Cancel();
        await _waitlistRepository.UpdateAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Ok(await MapToDtoAsync(entry, cancellationToken));
    }

    private async Task<WaitlistEntryDto> MapToDtoAsync(WaitlistEntry entry, CancellationToken cancellationToken)
    {
        string? patientName = null;
        string? doctorName = null;
        string? clinicName = null;

        var patient = await _patientRepository.GetByIdAsync(entry.PatientId, cancellationToken);
        if (patient is not null)
        {
            var pUser = await _userRepository.GetByIdAsync(patient.UserId, cancellationToken);
            if (pUser is not null) patientName = $"{pUser.FirstName} {pUser.LastName}".Trim();
        }

        var doctor = await _doctorRepository.GetByIdAsync(entry.DoctorId, cancellationToken);
        if (doctor is not null)
        {
            var dUser = await _userRepository.GetByIdAsync(doctor.UserId, cancellationToken);
            if (dUser is not null) doctorName = $"{dUser.FirstName} {dUser.LastName}".Trim();
        }

        var clinic = await _clinicRepository.GetByIdAsync(entry.ClinicId, cancellationToken);
        if (clinic is not null)
        {
            clinicName = clinic.NameEn;
        }

        return new WaitlistEntryDto(
            entry.Id,
            entry.TenantId,
            entry.ClinicId,
            entry.DoctorId,
            entry.PatientId,
            entry.PreferredDate,
            entry.PreferredStartTime,
            entry.PreferredEndTime,
            entry.Priority,
            entry.Notes,
            entry.Status.ToString(),
            entry.OfferedSlotStartUtc,
            entry.OfferedSlotEndUtc,
            entry.OfferExpiresAtUtc,
            entry.BookedAppointmentId,
            entry.CreatedAtUtc,
            patientName,
            doctorName,
            clinicName);
    }
}

public sealed record CreateWaitlistEntryRequest(
    Guid TenantId,
    Guid ClinicId,
    Guid DoctorId,
    Guid PatientId,
    DateOnly PreferredDate,
    TimeOnly? PreferredStartTime = null,
    TimeOnly? PreferredEndTime = null,
    int Priority = 1,
    string? Notes = null);

public sealed record OfferWaitlistSlotRequest(
    DateTime SlotStartUtc,
    DateTime SlotEndUtc,
    DateTime? OfferExpiresAtUtc = null);

public sealed record ClaimWaitlistSlotRequest(
    DateTime? StartTimeUtc = null,
    DateTime? EndTimeUtc = null);

public sealed record WaitlistEntryDto(
    Guid Id,
    Guid TenantId,
    Guid ClinicId,
    Guid DoctorId,
    Guid PatientId,
    DateOnly PreferredDate,
    TimeOnly? PreferredStartTime,
    TimeOnly? PreferredEndTime,
    int Priority,
    string? Notes,
    string Status,
    DateTime? OfferedSlotStartUtc,
    DateTime? OfferedSlotEndUtc,
    DateTime? OfferExpiresAtUtc,
    Guid? BookedAppointmentId,
    DateTime CreatedAtUtc,
    string? PatientName = null,
    string? DoctorName = null,
    string? ClinicName = null);

public sealed record WaitlistClaimResultDto(
    WaitlistEntryDto WaitlistEntry,
    AppointmentDto Appointment);

using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Appointments.Commands.BookAppointment;
using ClinicBooking.Application.Features.Appointments.DTOs;
using ClinicBooking.Application.Features.Appointments.Queries.GetAvailableSlots;
using ClinicBooking.Application.Features.Clinics.DTOs;
using ClinicBooking.Application.Features.Clinics.Queries.GetClinics;
using ClinicBooking.Application.Features.Doctors.DTOs;
using ClinicBooking.Application.Features.Doctors.Queries.GetDoctors;
using ClinicBooking.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClinicBooking.API.Controllers;

[Route("api/v1/public")]
public sealed class PublicBookingController : ApiControllerBase
{
    private readonly ITenantContext _tenantContext;

    public PublicBookingController(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    /// <summary>
    /// Discover public clinics and generate shareable social booking links (Facebook, WhatsApp, QR).
    /// </summary>
    [HttpGet("clinics")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<PublicClinicProfileDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PublicClinicProfileDto>>> GetPublicClinics(
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var clinicsResult = await Mediator.Send(new GetClinicsQuery(tenantId), cancellationToken);
        if (clinicsResult.IsFailure)
        {
            return HandleResult(clinicsResult);
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var profiles = clinicsResult.Value
            .Where(c => c.IsActive)
            .Select(c => BuildPublicProfile(c, baseUrl))
            .ToList();

        return Ok(profiles);
    }

    /// <summary>
    /// Get public clinic profile and social share links by clinic ID or slug.
    /// </summary>
    [HttpGet("clinics/{slugOrId}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicClinicProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PublicClinicProfileDto>> GetPublicClinicBySlugOrId(
        [FromRoute] string slugOrId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var clinicsResult = await Mediator.Send(new GetClinicsQuery(tenantId), cancellationToken);
        if (clinicsResult.IsFailure)
        {
            return HandleResult(clinicsResult);
        }

        var clinic = clinicsResult.Value.FirstOrDefault(c =>
            (Guid.TryParse(slugOrId, out var id) && c.Id == id) ||
            Slugify(c.Name).Equals(slugOrId, StringComparison.OrdinalIgnoreCase));

        if (clinic is null)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Clinic Not Found",
                Detail = $"Public clinic '{slugOrId}' was not found."
            });
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        return Ok(BuildPublicProfile(clinic, baseUrl));
    }

    /// <summary>
    /// List active doctors for a public clinic page.
    /// </summary>
    [HttpGet("clinics/{clinicId:guid}/doctors")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<DoctorDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<DoctorDto>>> GetClinicDoctors(
        [FromRoute] Guid clinicId,
        [FromQuery] Guid? specialtyId,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? Guid.Empty;
        var doctorsResult = await Mediator.Send(
            new GetDoctorsQuery(tenantId, specialtyId, clinicId, null),
            cancellationToken);

        return HandleResult(doctorsResult);
    }

    /// <summary>
    /// Retrieve available appointment slots for a public booking page.
    /// </summary>
    [HttpGet("clinics/{clinicId:guid}/available-slots")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(IReadOnlyList<AvailableSlotDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AvailableSlotDto>>> GetAvailableSlots(
        [FromRoute] Guid clinicId,
        [FromQuery] Guid doctorId,
        [FromQuery] DateOnly date,
        [FromQuery] Guid? tenantId,
        CancellationToken cancellationToken)
    {
        var effectiveTenantId = tenantId.HasValue && tenantId.Value != Guid.Empty
            ? tenantId.Value
            : (_tenantContext.TenantId ?? Guid.Empty);

        var result = await Mediator.Send(
            new GetAvailableSlotsQuery(effectiveTenantId, doctorId, clinicId, date),
            cancellationToken);

        return HandleResult(result);
    }

    /// <summary>
    /// Submit a booking with automatic UTM / Social-to-Booking attribution (Facebook, WhatsApp, Messenger, Website).
    /// </summary>
    [HttpPost("bookings")]
    [Authorize]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AppointmentDto>> CreateAttributedBooking(
        [FromBody] PublicAttributedBookingRequest request,
        [FromQuery(Name = "utm_source")] string? queryUtmSource,
        [FromQuery(Name = "utm_medium")] string? queryUtmMedium,
        [FromQuery(Name = "utm_campaign")] string? queryUtmCampaign,
        [FromQuery(Name = "ref")] string? queryRef,
        CancellationToken cancellationToken)
    {
        var tenantId = _tenantContext.TenantId ?? request.TenantId;
        var utmSource = request.UtmSource ?? queryUtmSource;
        var utmMedium = request.UtmMedium ?? queryUtmMedium;
        var utmCampaign = request.UtmCampaign ?? queryUtmCampaign;
        var externalRef = request.ExternalReference ?? queryRef;

        var resolvedSource = request.Source ?? InferSourceFromUtm(utmSource);

        var command = new BookAppointmentCommand(
            TenantId: tenantId,
            DoctorId: request.DoctorId,
            PatientId: request.PatientId,
            ClinicId: request.ClinicId,
            StartTimeUtc: request.StartTimeUtc,
            EndTimeUtc: request.EndTimeUtc,
            Notes: request.Notes,
            Source: resolvedSource,
            CreatedByStaffId: CurrentUserId,
            UtmSource: utmSource,
            UtmMedium: utmMedium,
            UtmCampaign: utmCampaign,
            ExternalReference: externalRef);

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    private static AppointmentSource InferSourceFromUtm(string? utmSource)
    {
        if (string.IsNullOrWhiteSpace(utmSource)) return AppointmentSource.Website;

        return utmSource.Trim().ToLowerInvariant() switch
        {
            "facebook" or "fb" => AppointmentSource.Facebook,
            "messenger" or "fb_messenger" => AppointmentSource.Messenger,
            "whatsapp" or "wa" => AppointmentSource.WhatsApp,
            "phone" or "call" => AppointmentSource.Phone,
            _ => AppointmentSource.Website
        };
    }

    private static PublicClinicProfileDto BuildPublicProfile(ClinicDto clinic, string baseUrl)
    {
        var slug = Slugify(clinic.Name);
        var bookingUrl = $"{baseUrl}/book/{slug}";
        var fbShareUrl = $"{bookingUrl}?utm_source=facebook&utm_medium=social&utm_campaign=clinic_page";
        var waShareUrl = $"{bookingUrl}?utm_source=whatsapp&utm_medium=chat&utm_campaign=direct_message";

        return new PublicClinicProfileDto(
            clinic.Id,
            clinic.TenantId,
            slug,
            clinic.Name,
            clinic.NameAr,
            clinic.Address,
            clinic.AddressAr,
            clinic.City,
            clinic.CityAr,
            clinic.PhoneNumber,
            bookingUrl,
            fbShareUrl,
            waShareUrl);
    }

    private static string Slugify(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "clinic";
        var chars = text.Trim().ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
            .ToArray();
        return string.Join('-', new string(chars).Split('-', StringSplitOptions.RemoveEmptyEntries));
    }
}

public sealed record PublicClinicProfileDto(
    Guid Id,
    Guid TenantId,
    string Slug,
    string Name,
    string? NameAr,
    string Address,
    string? AddressAr,
    string City,
    string? CityAr,
    string PhoneNumber,
    string ShareableBookingUrl,
    string FacebookTrackingUrl,
    string WhatsAppTrackingUrl);

public sealed record PublicAttributedBookingRequest(
    Guid TenantId,
    Guid DoctorId,
    Guid PatientId,
    Guid ClinicId,
    DateTime StartTimeUtc,
    DateTime EndTimeUtc,
    string? Notes = null,
    AppointmentSource? Source = null,
    string? UtmSource = null,
    string? UtmMedium = null,
    string? UtmCampaign = null,
    string? ExternalReference = null);

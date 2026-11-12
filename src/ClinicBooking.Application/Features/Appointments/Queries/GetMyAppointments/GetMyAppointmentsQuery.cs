using ClinicBooking.Application.Common.Localization;
using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Appointments.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Appointments.Queries.GetMyAppointments;

public sealed record GetMyAppointmentsQuery(Guid TenantId, Guid PatientId)
    : IRequest<Result<IReadOnlyList<AppointmentDto>>>;

public sealed class GetMyAppointmentsQueryHandler
    : IRequestHandler<GetMyAppointmentsQuery, Result<IReadOnlyList<AppointmentDto>>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDoctorRepository? _doctorRepository;
    private readonly IPatientRepository? _patientRepository;
    private readonly IClinicRepository? _clinicRepository;
    private readonly IUserRepository? _userRepository;
    private readonly ILocalizationService _localizationService;

    public GetMyAppointmentsQueryHandler(
        IAppointmentRepository appointmentRepository,
        IDoctorRepository? doctorRepository = null,
        IPatientRepository? patientRepository = null,
        IClinicRepository? clinicRepository = null,
        IUserRepository? userRepository = null,
        ILocalizationService? localizationService = null)
    {
        _appointmentRepository = appointmentRepository;
        _doctorRepository = doctorRepository;
        _patientRepository = patientRepository;
        _clinicRepository = clinicRepository;
        _userRepository = userRepository;
        _localizationService = localizationService ?? new LocalizationService();
    }

    public async Task<Result<IReadOnlyList<AppointmentDto>>> Handle(
        GetMyAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        var appointments = await _appointmentRepository.GetMyAppointmentsAsync(
            request.TenantId,
            request.PatientId,
            cancellationToken);

        var doctorMap = new Dictionary<Guid, string>();
        if (_doctorRepository is not null)
        {
            var doctors = await _doctorRepository.ListAsync(request.TenantId, cancellationToken: cancellationToken);
            foreach (var d in doctors)
            {
                string name = $"Dr. {d.LicenseNumber}";
                if (_userRepository is not null)
                {
                    var u = await _userRepository.GetByIdAsync(d.UserId, cancellationToken);
                    if (u is not null && !string.IsNullOrWhiteSpace(u.FullName))
                    {
                        name = $"Dr. {u.FullName}";
                    }
                }
                doctorMap[d.Id] = name;
            }
        }

        var patientMap = new Dictionary<Guid, string>();
        if (_patientRepository is not null && _userRepository is not null)
        {
            var patient = await _patientRepository.GetByIdAsync(request.PatientId, cancellationToken);
            if (patient is not null)
            {
                var u = await _userRepository.GetByIdAsync(patient.UserId, cancellationToken);
                if (u is not null && !string.IsNullOrWhiteSpace(u.FullName))
                {
                    patientMap[patient.Id] = u.FullName;
                }
            }
        }

        var clinicMap = new Dictionary<Guid, string>();
        if (_clinicRepository is not null)
        {
            var clinics = await _clinicRepository.ListAsync(request.TenantId, cancellationToken: cancellationToken);
            foreach (var c in clinics)
            {
                clinicMap[c.Id] = c.Name;
            }
        }

        var dtos = appointments.Select(a =>
        {
            doctorMap.TryGetValue(a.DoctorId, out var docName);
            patientMap.TryGetValue(a.PatientId, out var patName);
            clinicMap.TryGetValue(a.ClinicId, out var clinicName);

            return new AppointmentDto(
                a.Id,
                a.TenantId,
                a.DoctorId,
                a.PatientId,
                a.ClinicId,
                a.ScheduledRange.StartUtc,
                a.ScheduledRange.EndUtc,
                a.Status,
                a.Notes,
                a.CancellationReason,
                a.CancelledAtUtc,
                _localizationService.GetStatusName(a.Status),
                docName,
                patName,
                clinicName,
                Source: a.Source,
                CreatedByStaffId: a.CreatedByStaffId,
                UtmSource: a.UtmSource,
                UtmMedium: a.UtmMedium,
                UtmCampaign: a.UtmCampaign,
                ExternalReference: a.ExternalReference,
                QueueNumber: a.QueueNumber);
        }).ToList();

        return Result.Success<IReadOnlyList<AppointmentDto>>(dtos);
    }
}

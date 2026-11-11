using ClinicBooking.Application.Common.Localization;
using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Appointments.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Admin.Queries.GetAllAppointments;

public sealed record GetAllAppointmentsQuery(
    Guid TenantId,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    Guid? DoctorId = null,
    Guid? ClinicId = null) : IRequest<Result<IReadOnlyList<AppointmentDto>>>;

public sealed class GetAllAppointmentsQueryHandler
    : IRequestHandler<GetAllAppointmentsQuery, Result<IReadOnlyList<AppointmentDto>>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDoctorRepository? _doctorRepository;
    private readonly IPatientRepository? _patientRepository;
    private readonly IClinicRepository? _clinicRepository;
    private readonly IUserRepository? _userRepository;
    private readonly ILocalizationService _localizationService;

    public GetAllAppointmentsQueryHandler(
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
        GetAllAppointmentsQuery request,
        CancellationToken cancellationToken)
    {
        var appointments = await _appointmentRepository.GetAllAppointmentsAsync(
            request.TenantId,
            request.FromUtc,
            request.ToUtc,
            request.DoctorId,
            request.ClinicId,
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
        if (_patientRepository is not null)
        {
            var patients = await _patientRepository.ListAsync(request.TenantId, cancellationToken: cancellationToken);
            foreach (var p in patients)
            {
                string name = "Patient";
                if (_userRepository is not null)
                {
                    var u = await _userRepository.GetByIdAsync(p.UserId, cancellationToken);
                    if (u is not null && !string.IsNullOrWhiteSpace(u.FullName))
                    {
                        name = u.FullName;
                    }
                    else if (u is not null)
                    {
                        name = u.Email.Value;
                    }
                }
                patientMap[p.Id] = name;
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
                clinicName);
        }).ToList();

        return Result.Success<IReadOnlyList<AppointmentDto>>(dtos);
    }
}

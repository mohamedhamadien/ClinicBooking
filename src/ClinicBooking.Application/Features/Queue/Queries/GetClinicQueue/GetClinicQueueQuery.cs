using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Queue.DTOs;
using ClinicBooking.Domain.Enums;
using MediatR;

namespace ClinicBooking.Application.Features.Queue.Queries.GetClinicQueue;

public sealed record GetClinicQueueQuery(Guid TenantId, Guid ClinicId, DateOnly? Date = null) : IRequest<Result<IReadOnlyList<QueueItemDto>>>;

public sealed class GetClinicQueueQueryHandler : IRequestHandler<GetClinicQueueQuery, Result<IReadOnlyList<QueueItemDto>>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IClinicRepository _clinicRepository;
    private readonly IUserRepository _userRepository;

    public GetClinicQueueQueryHandler(
        IAppointmentRepository appointmentRepository,
        IPatientRepository patientRepository,
        IDoctorRepository doctorRepository,
        IClinicRepository clinicRepository,
        IUserRepository userRepository)
    {
        _appointmentRepository = appointmentRepository;
        _patientRepository = patientRepository;
        _doctorRepository = doctorRepository;
        _clinicRepository = clinicRepository;
        _userRepository = userRepository;
    }

    public async Task<Result<IReadOnlyList<QueueItemDto>>> Handle(GetClinicQueueQuery request, CancellationToken cancellationToken)
    {
        var targetDate = request.Date?.ToDateTime(TimeOnly.MinValue) ?? DateTime.UtcNow.Date;
        var nextDate = targetDate.AddDays(1);

        var clinic = await _clinicRepository.GetByIdAsync(request.ClinicId, cancellationToken);
        var clinicName = clinic?.NameEn ?? "Clinic";
        var effectiveTenantId = request.TenantId != Guid.Empty ? request.TenantId : (clinic?.TenantId ?? Guid.Empty);

        var appointments = await _appointmentRepository.GetAllAppointmentsAsync(
            effectiveTenantId,
            targetDate,
            nextDate,
            null,
            request.ClinicId,
            cancellationToken);

        // Filter for active/today appointments
        var queueAppointments = appointments
            .Where(a => a.Status is AppointmentStatus.Scheduled
                                or AppointmentStatus.Confirmed
                                or AppointmentStatus.CheckedIn
                                or AppointmentStatus.InConsultation
                                or AppointmentStatus.Completed)
            .OrderBy(a => a.QueueNumber ?? 999)
            .ThenBy(a => a.StartTimeUtc)
            .ToList();

        var dtos = new List<QueueItemDto>();

        foreach (var appt in queueAppointments)
        {
            var patient = await _patientRepository.GetByIdAsync(appt.PatientId, cancellationToken);
            string patientName = "Patient";
            if (patient is not null)
            {
                var user = await _userRepository.GetByIdAsync(patient.UserId, cancellationToken);
                patientName = user?.FullName ?? patientName;
            }

            var doctor = await _doctorRepository.GetByIdAsync(appt.DoctorId, cancellationToken);
            string doctorName = "Doctor";
            if (doctor is not null)
            {
                var user = await _userRepository.GetByIdAsync(doctor.UserId, cancellationToken);
                doctorName = user?.FullName ?? doctorName;
            }

            dtos.Add(new QueueItemDto(
                appt.Id,
                appt.QueueNumber,
                appt.PatientId,
                patientName,
                appt.DoctorId,
                doctorName,
                appt.ClinicId,
                clinicName,
                appt.StartTimeUtc,
                appt.Status));
        }

        return Result.Success<IReadOnlyList<QueueItemDto>>(dtos);
    }
}

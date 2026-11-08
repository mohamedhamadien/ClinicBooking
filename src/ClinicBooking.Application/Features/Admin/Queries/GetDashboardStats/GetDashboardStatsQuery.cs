using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Admin.DTOs;
using ClinicBooking.Domain.Enums;
using MediatR;

namespace ClinicBooking.Application.Features.Admin.Queries.GetDashboardStats;

public sealed record GetDashboardStatsQuery(Guid TenantId) : IRequest<Result<DashboardStatsDto>>;

public sealed class GetDashboardStatsQueryHandler
    : IRequestHandler<GetDashboardStatsQuery, Result<DashboardStatsDto>>
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IDoctorRepository _doctorRepository;
    private readonly IClinicRepository _clinicRepository;

    public GetDashboardStatsQueryHandler(
        IAppointmentRepository appointmentRepository,
        IDoctorRepository doctorRepository,
        IClinicRepository clinicRepository)
    {
        _appointmentRepository = appointmentRepository;
        _doctorRepository = doctorRepository;
        _clinicRepository = clinicRepository;
    }

    public async Task<Result<DashboardStatsDto>> Handle(
        GetDashboardStatsQuery request,
        CancellationToken cancellationToken)
    {
        var appointments = await _appointmentRepository.GetAllAppointmentsAsync(
            request.TenantId,
            fromUtc: null,
            toUtc: null,
            doctorId: null,
            cancellationToken);

        var doctors = await _doctorRepository.ListAsync(
            request.TenantId,
            specialtyId: null,
            clinicId: null,
            searchTerm: null,
            cancellationToken);

        var clinics = await _clinicRepository.ListAsync(request.TenantId, cancellationToken);

        var total = appointments.Count;
        var scheduled = appointments.Count(a => a.Status == AppointmentStatus.Scheduled);
        var confirmed = appointments.Count(a => a.Status == AppointmentStatus.Confirmed);
        var completed = appointments.Count(a => a.Status == AppointmentStatus.Completed);
        var cancelled = appointments.Count(a => a.Status is AppointmentStatus.CancelledByPatient
                                                         or AppointmentStatus.CancelledByDoctor
                                                         or AppointmentStatus.CancelledByAdmin);

        var dto = new DashboardStatsDto(
            total,
            scheduled,
            confirmed,
            completed,
            cancelled,
            doctors.Count,
            clinics.Count);

        return Result.Success(dto);
    }
}

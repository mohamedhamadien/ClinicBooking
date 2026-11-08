using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.Doctors.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Doctors.Queries.GetDoctorById;

public sealed record GetDoctorByIdQuery(Guid DoctorId) : IRequest<Result<DoctorDto>>;

public sealed class GetDoctorByIdQueryHandler : IRequestHandler<GetDoctorByIdQuery, Result<DoctorDto>>
{
    private readonly IDoctorRepository _doctorRepository;

    public GetDoctorByIdQueryHandler(IDoctorRepository doctorRepository)
    {
        _doctorRepository = doctorRepository;
    }

    public async Task<Result<DoctorDto>> Handle(GetDoctorByIdQuery request, CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdWithSchedulesAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
        {
            return Result.Failure<DoctorDto>(Error.NotFound("Doctor", request.DoctorId));
        }

        var dto = new DoctorDto(
            doctor.Id,
            doctor.TenantId,
            doctor.UserId,
            doctor.SpecialtyId,
            doctor.LicenseNumber,
            doctor.Bio,
            doctor.DefaultSlotDurationMinutes,
            doctor.RequiresApproval,
            doctor.IsActive,
            doctor.Availabilities.Select(a => new DoctorAvailabilityDto(
                a.Id,
                a.ClinicId,
                a.DayOfWeek,
                a.Shift.StartTime,
                a.Shift.EndTime,
                a.IsActive)).ToList());

        return Result.Success(dto);
    }
}

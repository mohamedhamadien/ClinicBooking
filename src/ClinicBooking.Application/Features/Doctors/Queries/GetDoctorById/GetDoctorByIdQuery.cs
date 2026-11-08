using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Doctors.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Doctors.Queries.GetDoctorById;

public sealed record GetDoctorByIdQuery(Guid DoctorId) : IRequest<Result<DoctorDto>>;

public sealed class GetDoctorByIdQueryHandler : IRequestHandler<GetDoctorByIdQuery, Result<DoctorDto>>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly ILocalizationService _localizationService;

    public GetDoctorByIdQueryHandler(
        IDoctorRepository doctorRepository,
        ILocalizationService localizationService)
    {
        _doctorRepository = doctorRepository;
        _localizationService = localizationService;
    }

    public async Task<Result<DoctorDto>> Handle(GetDoctorByIdQuery request, CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdWithSchedulesAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
        {
            return Result.Failure<DoctorDto>(Error.NotFound("Doctor", request.DoctorId));
        }

        var isArabic = _localizationService.IsArabic;

        var dto = new DoctorDto(
            doctor.Id,
            doctor.TenantId,
            doctor.UserId,
            doctor.SpecialtyId,
            doctor.LicenseNumber,
            doctor.BioEn,
            doctor.DefaultSlotDurationMinutes,
            doctor.RequiresApproval,
            doctor.IsActive,
            doctor.Availabilities.Select(a => new DoctorAvailabilityDto(
                a.Id,
                a.ClinicId,
                a.DayOfWeek,
                a.Shift.StartTime,
                a.Shift.EndTime,
                a.IsActive,
                _localizationService.GetDayOfWeekName(a.DayOfWeek))).ToList(),
            doctor.BioAr,
            doctor.GetLocalizedBio(isArabic));

        return Result.Success(dto);
    }
}

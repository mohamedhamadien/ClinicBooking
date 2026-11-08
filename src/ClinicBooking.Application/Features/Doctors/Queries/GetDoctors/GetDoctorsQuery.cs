using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Application.Features.Doctors.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.Doctors.Queries.GetDoctors;

public sealed record GetDoctorsQuery(
    Guid TenantId,
    Guid? SpecialtyId = null,
    Guid? ClinicId = null,
    string? SearchTerm = null) : IRequest<Result<IReadOnlyList<DoctorDto>>>;

public sealed class GetDoctorsQueryHandler : IRequestHandler<GetDoctorsQuery, Result<IReadOnlyList<DoctorDto>>>
{
    private readonly IDoctorRepository _doctorRepository;
    private readonly ILocalizationService _localizationService;

    public GetDoctorsQueryHandler(
        IDoctorRepository doctorRepository,
        ILocalizationService localizationService)
    {
        _doctorRepository = doctorRepository;
        _localizationService = localizationService;
    }

    public async Task<Result<IReadOnlyList<DoctorDto>>> Handle(GetDoctorsQuery request, CancellationToken cancellationToken)
    {
        var doctors = await _doctorRepository.ListAsync(
            request.TenantId,
            request.SpecialtyId,
            request.ClinicId,
            request.SearchTerm,
            cancellationToken);

        var isArabic = _localizationService.IsArabic;

        var dtos = doctors.Select(d => new DoctorDto(
            d.Id,
            d.TenantId,
            d.UserId,
            d.SpecialtyId,
            d.LicenseNumber,
            d.BioEn,
            d.DefaultSlotDurationMinutes,
            d.RequiresApproval,
            d.IsActive,
            d.Availabilities.Select(a => new DoctorAvailabilityDto(
                a.Id,
                a.ClinicId,
                a.DayOfWeek,
                a.Shift.StartTime,
                a.Shift.EndTime,
                a.IsActive,
                _localizationService.GetDayOfWeekName(a.DayOfWeek))).ToList(),
            d.BioAr,
            d.GetLocalizedBio(isArabic)
        )).ToList();

        return Result.Success<IReadOnlyList<DoctorDto>>(dtos);
    }
}

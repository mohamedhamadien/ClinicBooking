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
    private readonly IClinicRepository? _clinicRepository;
    private readonly ISpecialtyRepository? _specialtyRepository;
    private readonly IUserRepository? _userRepository;

    public GetDoctorsQueryHandler(
        IDoctorRepository doctorRepository,
        ILocalizationService localizationService,
        IClinicRepository? clinicRepository = null,
        ISpecialtyRepository? specialtyRepository = null,
        IUserRepository? userRepository = null)
    {
        _doctorRepository = doctorRepository;
        _localizationService = localizationService;
        _clinicRepository = clinicRepository;
        _specialtyRepository = specialtyRepository;
        _userRepository = userRepository;
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

        var clinics = _clinicRepository is not null
            ? await _clinicRepository.ListAsync(request.TenantId, cancellationToken)
            : [];
        var clinicMap = clinics.ToDictionary(c => c.Id);

        var specialties = _specialtyRepository is not null
            ? await _specialtyRepository.ListAsync(request.TenantId, cancellationToken)
            : [];
        var specialtyMap = specialties.ToDictionary(s => s.Id);

        var dtos = new List<DoctorDto>();

        foreach (var d in doctors)
        {
            string? fullName = null;
            string? email = null;
            if (_userRepository is not null)
            {
                var user = await _userRepository.GetByIdAsync(d.UserId, cancellationToken);
                fullName = user?.FullName;
                email = user?.Email.Value;
            }

            var specialtyName = specialtyMap.TryGetValue(d.SpecialtyId, out var spec)
                ? spec.GetLocalizedName(isArabic)
                : null;

            var docClinics = d.DoctorClinics.Select(dc =>
            {
                clinicMap.TryGetValue(dc.ClinicId, out var c);
                return new DoctorClinicDto(
                    dc.ClinicId,
                    c?.GetLocalizedName(isArabic) ?? "Clinic Branch",
                    c?.GetLocalizedCity(isArabic) ?? "City",
                    dc.ConsultationFee,
                    dc.IsPrimary);
            }).ToList();

            var availabilities = d.Availabilities.Select(a =>
            {
                clinicMap.TryGetValue(a.ClinicId, out var c);
                return new DoctorAvailabilityDto(
                    a.Id,
                    a.ClinicId,
                    a.DayOfWeek,
                    a.Shift.StartTime,
                    a.Shift.EndTime,
                    a.IsActive,
                    _localizationService.GetDayOfWeekName(a.DayOfWeek),
                    c?.GetLocalizedName(isArabic),
                    new DoctorShiftDto(a.Shift.StartTime, a.Shift.EndTime));
            }).ToList();

            dtos.Add(new DoctorDto(
                d.Id,
                d.TenantId,
                d.UserId,
                d.SpecialtyId,
                d.LicenseNumber,
                d.BioEn,
                d.DefaultSlotDurationMinutes,
                d.RequiresApproval,
                d.IsActive,
                availabilities,
                d.BioAr,
                d.GetLocalizedBio(isArabic),
                fullName,
                email,
                specialtyName,
                docClinics));
        }

        return Result.Success<IReadOnlyList<DoctorDto>>(dtos);
    }
}

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
    private readonly IClinicRepository? _clinicRepository;
    private readonly ISpecialtyRepository? _specialtyRepository;
    private readonly IUserRepository? _userRepository;

    public GetDoctorByIdQueryHandler(
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

    public async Task<Result<DoctorDto>> Handle(GetDoctorByIdQuery request, CancellationToken cancellationToken)
    {
        var doctor = await _doctorRepository.GetByIdWithSchedulesAsync(request.DoctorId, cancellationToken);
        if (doctor is null)
        {
            return Result.Failure<DoctorDto>(Error.NotFound("Doctor", request.DoctorId));
        }

        var isArabic = _localizationService.IsArabic;

        string? fullName = null;
        string? email = null;
        if (_userRepository is not null)
        {
            var user = await _userRepository.GetByIdAsync(doctor.UserId, cancellationToken);
            fullName = user?.FullName;
            email = user?.Email.Value;
        }

        string? specialtyName = null;
        if (_specialtyRepository is not null)
        {
            var specialty = await _specialtyRepository.GetByIdAsync(doctor.SpecialtyId, cancellationToken);
            specialtyName = specialty?.GetLocalizedName(isArabic);
        }

        var clinics = _clinicRepository is not null
            ? await _clinicRepository.ListAsync(doctor.TenantId, cancellationToken)
            : [];
        var clinicMap = clinics.ToDictionary(c => c.Id);

        var docClinics = doctor.DoctorClinics.Select(dc =>
        {
            clinicMap.TryGetValue(dc.ClinicId, out var c);
            return new DoctorClinicDto(
                dc.ClinicId,
                c?.GetLocalizedName(isArabic) ?? "Clinic Branch",
                c?.GetLocalizedCity(isArabic) ?? "City",
                dc.ConsultationFee,
                dc.IsPrimary);
        }).ToList();

        var availabilities = doctor.Availabilities.Select(a =>
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
            availabilities,
            doctor.BioAr,
            doctor.GetLocalizedBio(isArabic),
            fullName,
            email,
            specialtyName,
            docClinics);

        return Result.Success(dto);
    }
}

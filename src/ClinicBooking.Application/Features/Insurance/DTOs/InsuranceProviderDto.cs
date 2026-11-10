namespace ClinicBooking.Application.Features.Insurance.DTOs;

public sealed record InsuranceProviderDto(
    Guid Id,
    string Name,
    string? NameAr,
    string Code,
    decimal DefaultCoveragePercentage,
    string? ContactPhone,
    bool IsActive);

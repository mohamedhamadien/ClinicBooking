namespace ClinicBooking.Application.Features.Clinics.DTOs;

public sealed record ClinicDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string Address,
    string City,
    string PhoneNumber,
    bool IsActive,
    string? NameAr = null,
    string? AddressAr = null,
    string? CityAr = null,
    string? LocalizedName = null,
    string? LocalizedAddress = null,
    string? LocalizedCity = null,
    DateTime? SubscriptionStartDateUtc = null,
    DateTime? SubscriptionEndDateUtc = null,
    string? SubscriptionPlan = null,
    bool? IsSubscriptionActive = null,
    int? SubscriptionDaysRemaining = null);

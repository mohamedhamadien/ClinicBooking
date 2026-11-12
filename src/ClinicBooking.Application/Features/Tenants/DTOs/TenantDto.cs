namespace ClinicBooking.Application.Features.Tenants.DTOs;

public sealed record TenantDto(
    Guid Id,
    string Name,
    string Identifier,
    bool IsActive,
    int CancellationNoticeHours,
    bool RequiresDoctorApproval,
    string? NameAr = null,
    string? LocalizedName = null,
    DateTime? SubscriptionStartDateUtc = null,
    DateTime? SubscriptionEndDateUtc = null,
    string? SubscriptionPlan = null,
    bool? IsSubscriptionActive = null,
    int? SubscriptionDaysRemaining = null);

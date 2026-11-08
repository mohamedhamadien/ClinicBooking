using ClinicBooking.Domain.Enums;

namespace ClinicBooking.Application.Contracts.Services;

public interface ILocalizationService
{
    string CurrentCulture { get; }
    bool IsArabic { get; }
    string GetString(string key);
    string GetString(string key, params object[] args);
    string GetStatusName(AppointmentStatus status);
    string GetGenderName(Gender gender);
    string GetDayOfWeekName(DayOfWeek dayOfWeek);
}

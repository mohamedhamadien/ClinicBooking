using System.Text.Json.Serialization;

namespace ClinicBooking.Domain.Enums;

/// <summary>
/// Distinguishes between a standard new consultation, a follow-up visit (إعادة كشف),
/// and an in-shift clinical procedure or surgery (عملية / إجراء طبي وسط الكشوفات).
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppointmentVisitType
{
    Consultation = 1,
    FollowUp = 2,
    Procedure = 3
}

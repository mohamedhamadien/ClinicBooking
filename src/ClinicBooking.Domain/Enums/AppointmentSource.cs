using System.Text.Json.Serialization;

namespace ClinicBooking.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AppointmentSource
{
    Website = 1,
    Facebook = 2,
    Messenger = 3,
    WhatsApp = 4,
    Phone = 5,
    WalkIn = 6,
    Receptionist = 7,
    Admin = 8
}

namespace ClinicBooking.Application.Common.Localization;

public static class LocalizationKeys
{
    // Appointments
    public const string AppointmentDoctorUnavailable = "Appointment.DoctorUnavailable";
    public const string AppointmentSlotAlreadyBooked = "Appointment.SlotAlreadyBooked";
    public const string AppointmentPastAppointment = "Appointment.PastAppointment";
    public const string AppointmentPatientOverlapping = "Appointment.PatientOverlapping";
    public const string AppointmentNotFound = "Appointment.NotFound";
    public const string AppointmentCancellationNoticeExpired = "Appointment.CancellationNoticeExpired";
    public const string AppointmentInvalidStatusTransition = "Appointment.InvalidStatusTransition";
    public const string AppointmentBookingSuccess = "Appointment.BookingSuccess";
    public const string AppointmentCancellationSuccess = "Appointment.CancellationSuccess";

    // Entities
    public const string DoctorNotFound = "Doctor.NotFound";
    public const string PatientNotFound = "Patient.NotFound";
    public const string ClinicNotFound = "Clinic.NotFound";
    public const string SpecialtyNotFound = "Specialty.NotFound";
    public const string TenantNotFound = "Tenant.NotFound";
    public const string TenantDuplicateIdentifier = "Tenant.DuplicateIdentifier";

    // Authentication
    public const string AuthInvalidCredentials = "Auth.InvalidCredentials";
    public const string AuthEmailAlreadyRegistered = "Auth.EmailAlreadyRegistered";
    public const string AuthInvalidToken = "Auth.InvalidToken";
    public const string AuthUnauthorized = "Auth.Unauthorized";
    public const string AuthForbidden = "Auth.Forbidden";

    // Validation
    public const string ValidationRequired = "Validation.Required";
    public const string ValidationInvalidEmail = "Validation.InvalidEmail";
    public const string ValidationInvalidPhone = "Validation.InvalidPhone";
    public const string ValidationSlotDurationRange = "Validation.SlotDurationRange";
    public const string ValidationCancellationNoticeRange = "Validation.CancellationNoticeRange";
    public const string ValidationIdentifierFormat = "Validation.IdentifierFormat";
}

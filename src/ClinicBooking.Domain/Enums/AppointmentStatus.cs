namespace ClinicBooking.Domain.Enums;

public enum AppointmentStatus
{
    Scheduled = 1,
    PendingApproval = 2,
    Confirmed = 3,
    Completed = 4,
    CancelledByPatient = 5,
    CancelledByDoctor = 6,
    CancelledByAdmin = 7,
    Rejected = 8,
    NoShow = 9,
    CheckedIn = 10,
    InConsultation = 11
}

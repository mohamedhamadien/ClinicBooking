using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Exceptions;

namespace ClinicBooking.Domain.Entities;

public sealed class ConsultationNote : AggregateRoot<Guid>, IAuditableEntity, IMustHaveTenant
{
    public Guid TenantId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid ClinicId { get; private set; }
    public string ChiefComplaint { get; private set; } = default!;
    public string ExaminationNotes { get; private set; } = default!;
    public string Diagnosis { get; private set; } = default!;
    public string TreatmentPlan { get; private set; } = default!;
    public DateTime? FollowUpDateUtc { get; private set; }
    public string? LabRequests { get; private set; }
    public DateTime ConsultationDateUtc { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private ConsultationNote() { }

    public ConsultationNote(
        Guid id,
        Guid tenantId,
        Guid appointmentId,
        Guid patientId,
        Guid doctorId,
        Guid clinicId,
        string chiefComplaint,
        string examinationNotes,
        string diagnosis,
        string treatmentPlan,
        DateTime consultationDateUtc,
        DateTime? followUpDateUtc = null,
        string? labRequests = null) : base(id)
    {
        if (tenantId == Guid.Empty) throw new DomainValidationException("TenantId is required.");
        if (appointmentId == Guid.Empty) throw new DomainValidationException("AppointmentId is required.");
        if (patientId == Guid.Empty) throw new DomainValidationException("PatientId is required.");
        if (doctorId == Guid.Empty) throw new DomainValidationException("DoctorId is required.");

        TenantId = tenantId;
        AppointmentId = appointmentId;
        PatientId = patientId;
        DoctorId = doctorId;
        ClinicId = clinicId;
        ChiefComplaint = chiefComplaint?.Trim() ?? string.Empty;
        ExaminationNotes = examinationNotes?.Trim() ?? string.Empty;
        Diagnosis = string.IsNullOrWhiteSpace(diagnosis) ? throw new DomainValidationException("Diagnosis is required.") : diagnosis.Trim();
        TreatmentPlan = treatmentPlan?.Trim() ?? string.Empty;
        ConsultationDateUtc = DateTime.SpecifyKind(consultationDateUtc, DateTimeKind.Utc);
        FollowUpDateUtc = followUpDateUtc.HasValue ? DateTime.SpecifyKind(followUpDateUtc.Value, DateTimeKind.Utc) : null;
        LabRequests = labRequests?.Trim();
    }

    public static ConsultationNote Create(
        Guid tenantId,
        Guid appointmentId,
        Guid patientId,
        Guid doctorId,
        Guid clinicId,
        string chiefComplaint,
        string examinationNotes,
        string diagnosis,
        string treatmentPlan,
        DateTime consultationDateUtc,
        DateTime? followUpDateUtc = null,
        string? labRequests = null)
    {
        return new ConsultationNote(
            Guid.NewGuid(),
            tenantId,
            appointmentId,
            patientId,
            doctorId,
            clinicId,
            chiefComplaint,
            examinationNotes,
            diagnosis,
            treatmentPlan,
            consultationDateUtc,
            followUpDateUtc,
            labRequests);
    }
}

using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Exceptions;

namespace ClinicBooking.Domain.Entities;

public sealed class Prescription : AggregateRoot<Guid>, IAuditableEntity, IMustHaveTenant
{
    public Guid TenantId { get; private set; }
    public Guid? ConsultationId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid ClinicId { get; private set; }
    public string PrescriptionNumber { get; private set; } = default!;
    public string Diagnosis { get; private set; } = default!;
    public string? GeneralInstructions { get; private set; }
    public DateTime IssuedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private readonly List<PrescriptionItem> _items = [];
    public IReadOnlyCollection<PrescriptionItem> Items => _items.AsReadOnly();

    private Prescription() { }

    public Prescription(
        Guid id,
        Guid tenantId,
        Guid? consultationId,
        Guid patientId,
        Guid doctorId,
        Guid clinicId,
        string prescriptionNumber,
        string diagnosis,
        string? generalInstructions,
        DateTime issuedAtUtc) : base(id)
    {
        TenantId = tenantId;
        ConsultationId = consultationId;
        PatientId = patientId;
        DoctorId = doctorId;
        ClinicId = clinicId;
        PrescriptionNumber = string.IsNullOrWhiteSpace(prescriptionNumber)
            ? $"RX-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}"
            : prescriptionNumber.Trim();
        Diagnosis = diagnosis?.Trim() ?? string.Empty;
        GeneralInstructions = generalInstructions?.Trim();
        IssuedAtUtc = DateTime.SpecifyKind(issuedAtUtc, DateTimeKind.Utc);
    }

    public static Prescription Create(
        Guid tenantId,
        Guid? consultationId,
        Guid patientId,
        Guid doctorId,
        Guid clinicId,
        string diagnosis,
        string? generalInstructions = null,
        string? prescriptionNumber = null)
    {
        return new Prescription(
            Guid.NewGuid(),
            tenantId,
            consultationId,
            patientId,
            doctorId,
            clinicId,
            prescriptionNumber ?? string.Empty,
            diagnosis,
            generalInstructions,
            DateTime.UtcNow);
    }

    public void AddItem(string medicationName, string dosage, string frequency, string duration, string? instructions = null)
    {
        if (string.IsNullOrWhiteSpace(medicationName))
        {
            throw new DomainValidationException("Medication name is required.");
        }

        _items.Add(new PrescriptionItem(
            Guid.NewGuid(),
            Id,
            medicationName,
            dosage,
            frequency,
            duration,
            instructions));
    }
}

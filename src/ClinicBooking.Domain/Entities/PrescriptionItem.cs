using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Entities;

public sealed class PrescriptionItem : Entity<Guid>
{
    public Guid PrescriptionId { get; private set; }
    public string MedicationName { get; private set; } = default!;
    public string Dosage { get; private set; } = default!;
    public string Frequency { get; private set; } = default!;
    public string Duration { get; private set; } = default!;
    public string? Instructions { get; private set; }

    private PrescriptionItem() { }

    public PrescriptionItem(
        Guid id,
        Guid prescriptionId,
        string medicationName,
        string dosage,
        string frequency,
        string duration,
        string? instructions = null) : base(id)
    {
        PrescriptionId = prescriptionId;
        MedicationName = medicationName.Trim();
        Dosage = dosage.Trim();
        Frequency = frequency.Trim();
        Duration = duration.Trim();
        Instructions = instructions?.Trim();
    }
}

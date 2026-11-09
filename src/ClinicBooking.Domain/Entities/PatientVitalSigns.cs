using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Entities;

public sealed class PatientVitalSigns : Entity<Guid>, IAuditableEntity, IMustHaveTenant
{
    public Guid TenantId { get; private set; }
    public Guid PatientId { get; private set; }
    public Guid? ConsultationId { get; private set; }
    public int? SystolicBp { get; private set; }
    public int? DiastolicBp { get; private set; }
    public int? HeartRate { get; private set; }
    public decimal? TemperatureCelsius { get; private set; }
    public decimal? WeightKg { get; private set; }
    public decimal? HeightCm { get; private set; }
    public decimal? BloodGlucoseMgDl { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    public decimal? Bmi => (WeightKg.HasValue && HeightCm.HasValue && HeightCm > 0)
        ? Math.Round(WeightKg.Value / ((HeightCm.Value / 100m) * (HeightCm.Value / 100m)), 1)
        : null;

    private PatientVitalSigns() { }

    public PatientVitalSigns(
        Guid id,
        Guid tenantId,
        Guid patientId,
        Guid? consultationId,
        int? systolicBp,
        int? diastolicBp,
        int? heartRate,
        decimal? temperatureCelsius,
        decimal? weightKg,
        decimal? heightCm,
        decimal? bloodGlucoseMgDl,
        DateTime recordedAtUtc) : base(id)
    {
        TenantId = tenantId;
        PatientId = patientId;
        ConsultationId = consultationId;
        SystolicBp = systolicBp;
        DiastolicBp = diastolicBp;
        HeartRate = heartRate;
        TemperatureCelsius = temperatureCelsius;
        WeightKg = weightKg;
        HeightCm = heightCm;
        BloodGlucoseMgDl = bloodGlucoseMgDl;
        RecordedAtUtc = DateTime.SpecifyKind(recordedAtUtc, DateTimeKind.Utc);
    }

    public static PatientVitalSigns Create(
        Guid tenantId,
        Guid patientId,
        Guid? consultationId,
        int? systolicBp,
        int? diastolicBp,
        int? heartRate,
        decimal? temperatureCelsius,
        decimal? weightKg,
        decimal? heightCm,
        decimal? bloodGlucoseMgDl,
        DateTime recordedAtUtc)
    {
        return new PatientVitalSigns(
            Guid.NewGuid(),
            tenantId,
            patientId,
            consultationId,
            systolicBp,
            diastolicBp,
            heartRate,
            temperatureCelsius,
            weightKg,
            heightCm,
            bloodGlucoseMgDl,
            recordedAtUtc);
    }
}

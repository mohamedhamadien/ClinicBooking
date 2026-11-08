using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Entities;

public sealed class DoctorClinic : Entity<Guid>, IAuditableEntity, IMustHaveTenant
{
    public Guid TenantId { get; private set; }
    public Guid DoctorId { get; private set; }
    public Guid ClinicId { get; private set; }
    public decimal ConsultationFee { get; private set; }
    public bool IsPrimary { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private DoctorClinic() { }

    public DoctorClinic(Guid id, Guid tenantId, Guid doctorId, Guid clinicId, decimal consultationFee, bool isPrimary = false)
        : base(id)
    {
        TenantId = tenantId;
        DoctorId = doctorId;
        ClinicId = clinicId;
        ConsultationFee = consultationFee >= 0 ? consultationFee : 0;
        IsPrimary = isPrimary;
    }

    public void UpdateFee(decimal newFee)
    {
        ConsultationFee = newFee >= 0 ? newFee : 0;
    }

    public void SetPrimary(bool isPrimary) => IsPrimary = isPrimary;
}

using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Exceptions;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Domain.Entities;

public sealed class Clinic : Entity<Guid>, IAuditableEntity, IMustHaveTenant
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string Address { get; private set; } = default!;
    public string City { get; private set; } = default!;
    public PhoneNumber PhoneNumber { get; private set; } = default!;
    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private Clinic() { }

    public Clinic(
        Guid id,
        Guid tenantId,
        string name,
        string address,
        string city,
        PhoneNumber phoneNumber,
        bool isActive = true) : base(id)
    {
        if (tenantId == Guid.Empty) throw new DomainValidationException("Tenant ID cannot be empty.");
        TenantId = tenantId;
        SetName(name);
        Address = address?.Trim() ?? string.Empty;
        City = city?.Trim() ?? string.Empty;
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        IsActive = isActive;
    }

    public static Clinic Create(Guid tenantId, string name, string address, string city, string phoneNumber)
    {
        return new Clinic(
            Guid.NewGuid(),
            tenantId,
            name,
            address,
            city,
            PhoneNumber.Create(phoneNumber),
            true);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Clinic name cannot be empty.");
        }

        Name = name.Trim();
    }

    public void Update(string name, string address, string city, PhoneNumber phoneNumber, bool isActive)
    {
        SetName(name);
        Address = address?.Trim() ?? string.Empty;
        City = city?.Trim() ?? string.Empty;
        PhoneNumber = phoneNumber ?? throw new ArgumentNullException(nameof(phoneNumber));
        IsActive = isActive;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

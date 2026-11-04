using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Exceptions;

namespace ClinicBooking.Domain.Entities;

public sealed class Specialty : Entity<Guid>, IAuditableEntity
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private Specialty() { }

    public Specialty(Guid id, string name, string description, bool isActive = true) : base(id)
    {
        SetName(name);
        Description = description?.Trim() ?? string.Empty;
        IsActive = isActive;
    }

    public static Specialty Create(string name, string description)
    {
        return new Specialty(Guid.NewGuid(), name, description, true);
    }

    public void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Specialty name cannot be empty.");
        }

        Name = name.Trim();
    }

    public void Update(string name, string description, bool isActive)
    {
        SetName(name);
        Description = description?.Trim() ?? string.Empty;
        IsActive = isActive;
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}

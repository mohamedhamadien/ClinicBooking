using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.Exceptions;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Domain.Entities;

public sealed class Patient : AggregateRoot<Guid>, IAuditableEntity, IMustHaveTenant, ISoftDeletable
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public PhoneNumber? EmergencyContact { get; private set; }
    public string? BloodGroup { get; private set; }
    public string? Allergies { get; private set; }
    public string? ChronicConditions { get; private set; }
    public string? MedicalNotes { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private Patient() { }

    public Patient(
        Guid id,
        Guid tenantId,
        Guid userId,
        DateOnly dateOfBirth,
        Gender gender,
        PhoneNumber? emergencyContact = null) : base(id)
    {
        if (tenantId == Guid.Empty) throw new DomainValidationException("Tenant ID cannot be empty.");
        TenantId = tenantId;
        UserId = userId;
        SetDateOfBirth(dateOfBirth);
        Gender = gender;
        EmergencyContact = emergencyContact;
    }

    public static Patient Create(
        Guid tenantId,
        Guid userId,
        DateOnly dateOfBirth,
        Gender gender,
        string? emergencyContact = null)
    {
        return new Patient(
            Guid.NewGuid(),
            tenantId,
            userId,
            dateOfBirth,
            gender,
            !string.IsNullOrWhiteSpace(emergencyContact) ? PhoneNumber.Create(emergencyContact) : null);
    }

    public void SetDateOfBirth(DateOnly dateOfBirth)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (dateOfBirth > today)
        {
            throw new DomainValidationException("Patient date of birth cannot be in the future.");
        }

        DateOfBirth = dateOfBirth;
    }

    public void UpdateProfile(DateOnly dateOfBirth, Gender gender, PhoneNumber? emergencyContact)
    {
        SetDateOfBirth(dateOfBirth);
        Gender = gender;
        EmergencyContact = emergencyContact;
    }

    public void UpdateMedicalHistory(string? bloodGroup, string? allergies, string? chronicConditions, string? medicalNotes)
    {
        BloodGroup = bloodGroup?.Trim();
        Allergies = allergies?.Trim();
        ChronicConditions = chronicConditions?.Trim();
        MedicalNotes = medicalNotes?.Trim();
    }
}

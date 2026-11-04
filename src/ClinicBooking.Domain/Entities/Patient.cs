using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Enums;
using ClinicBooking.Domain.Exceptions;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Domain.Entities;

public sealed class Patient : AggregateRoot<Guid>, IAuditableEntity
{
    public Guid UserId { get; private set; }
    public DateOnly DateOfBirth { get; private set; }
    public Gender Gender { get; private set; }
    public PhoneNumber? EmergencyContact { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private Patient() { }

    public Patient(
        Guid id,
        Guid userId,
        DateOnly dateOfBirth,
        Gender gender,
        PhoneNumber? emergencyContact = null) : base(id)
    {
        UserId = userId;
        SetDateOfBirth(dateOfBirth);
        Gender = gender;
        EmergencyContact = emergencyContact;
    }

    public static Patient Create(
        Guid userId,
        DateOnly dateOfBirth,
        Gender gender,
        string? emergencyContact = null)
    {
        return new Patient(
            Guid.NewGuid(),
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
}

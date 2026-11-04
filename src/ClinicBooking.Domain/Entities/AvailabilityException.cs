using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Exceptions;
using ClinicBooking.Domain.ValueObjects;

namespace ClinicBooking.Domain.Entities;

public sealed class AvailabilityException : Entity<Guid>, IAuditableEntity
{
    public Guid DoctorId { get; private set; }
    public DateTimeRange Range { get; private set; } = default!;
    public string Reason { get; private set; } = default!;
    public bool IsBlocked { get; private set; }

    public DateTime CreatedAtUtc { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    private AvailabilityException() { }

    public AvailabilityException(
        Guid id,
        Guid doctorId,
        DateTimeRange range,
        string reason,
        bool isBlocked = true) : base(id)
    {
        DoctorId = doctorId;
        Range = range ?? throw new ArgumentNullException(nameof(range));
        Reason = reason?.Trim() ?? string.Empty;
        IsBlocked = isBlocked;
    }

    public static AvailabilityException CreateBlock(
        Guid doctorId,
        DateTime startUtc,
        DateTime endUtc,
        string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainValidationException("Reason for blocking time must be provided.");
        }

        return new AvailabilityException(
            Guid.NewGuid(),
            doctorId,
            DateTimeRange.Create(startUtc, endUtc),
            reason,
            isBlocked: true);
    }

    public void Update(DateTimeRange range, string reason, bool isBlocked)
    {
        Range = range ?? throw new ArgumentNullException(nameof(range));
        Reason = reason?.Trim() ?? string.Empty;
        IsBlocked = isBlocked;
    }
}

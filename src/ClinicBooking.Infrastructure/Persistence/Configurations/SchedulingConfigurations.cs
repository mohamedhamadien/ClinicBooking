using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBooking.Infrastructure.Persistence.Configurations;

internal sealed class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedNever();

        builder.Property(d => d.LicenseNumber).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(d => d.BioEn).HasMaxLength(2000).IsRequired();
        builder.Property(d => d.BioAr).HasMaxLength(2000).IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(d => d.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithOne().HasForeignKey<Doctor>(d => d.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Specialty>().WithMany().HasForeignKey(d => d.SpecialtyId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => d.UserId).IsUnique().HasDatabaseName("UX_Doctors_UserId");
        builder.HasIndex(d => new { d.TenantId, d.LicenseNumber }).IsUnique().HasDatabaseName("UX_Doctors_Tenant_License");
        builder.HasIndex(d => new { d.TenantId, d.SpecialtyId }).HasDatabaseName("IX_Doctors_Tenant_Specialty");

        // Aggregate children are owned by the Doctor lifecycle (cascade) and accessed through backing fields.
        builder.HasMany(d => d.Availabilities).WithOne().HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(d => d.Exceptions).WithOne().HasForeignKey(e => e.DoctorId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(d => d.DoctorClinics).WithOne().HasForeignKey(dc => dc.DoctorId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(d => d.Availabilities).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(d => d.Exceptions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(d => d.DoctorClinics).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.ConfigureAudit();
        builder.Ignore(d => d.DomainEvents);
    }
}

internal sealed class DoctorAvailabilityConfiguration : IEntityTypeConfiguration<DoctorAvailability>
{
    public void Configure(EntityTypeBuilder<DoctorAvailability> builder)
    {
        builder.ToTable("DoctorAvailabilities");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.ComplexProperty(a => a.Shift, shift =>
        {
            shift.Property(s => s.StartTime).HasColumnName("StartTime").HasColumnType("time(0)");
            shift.Property(s => s.EndTime).HasColumnName("EndTime").HasColumnType("time(0)");
        });

        builder.HasOne<Clinic>().WithMany().HasForeignKey(a => a.ClinicId).OnDelete(DeleteBehavior.Restrict);

        // Slot generation reads "doctor X on weekday Y" — the hottest availability query.
        builder.HasIndex(a => new { a.TenantId, a.DoctorId, a.DayOfWeek })
            .HasDatabaseName("IX_DoctorAvailabilities_Doctor_Day")
            .IncludeProperties(a => new { a.ClinicId, a.IsActive });

        builder.ConfigureAudit();
    }
}

internal sealed class AvailabilityExceptionConfiguration : IEntityTypeConfiguration<AvailabilityException>
{
    public void Configure(EntityTypeBuilder<AvailabilityException> builder)
    {
        builder.ToTable("AvailabilityExceptions");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.ComplexProperty(e => e.Range, range =>
        {
            range.Property(r => r.StartUtc).HasColumnName("StartTimeUtc");
            range.Property(r => r.EndUtc).HasColumnName("EndTimeUtc");
        });

        builder.Property(e => e.Reason).HasMaxLength(300).IsRequired();
        builder.HasIndex(e => new { e.TenantId, e.DoctorId }).HasDatabaseName("IX_AvailabilityExceptions_Doctor");

        builder.ConfigureAudit();
    }
}

internal sealed class DoctorClinicConfiguration : IEntityTypeConfiguration<DoctorClinic>
{
    public void Configure(EntityTypeBuilder<DoctorClinic> builder)
    {
        builder.ToTable("DoctorClinics");
        builder.HasKey(dc => dc.Id);
        builder.Property(dc => dc.Id).ValueGeneratedNever();

        builder.Property(dc => dc.ConsultationFee).HasPrecision(10, 2);
        builder.HasOne<Clinic>().WithMany().HasForeignKey(dc => dc.ClinicId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(dc => new { dc.DoctorId, dc.ClinicId }).IsUnique().HasDatabaseName("UX_DoctorClinics_Doctor_Clinic");

        builder.ConfigureAudit();
    }
}

internal sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments", t =>
            t.HasCheckConstraint("CK_Appointments_EndAfterStart", "[EndTimeUtc] > [StartTimeUtc]"));

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.StartTimeUtc).IsRequired();
        builder.Property(a => a.EndTimeUtc).IsRequired();

        // Stored as readable strings so the filtered index predicate is self-documenting.
        builder.Property(a => a.Status).HasConversion<string>().HasMaxLength(32).IsUnicode(false).IsRequired();
        builder.Property(a => a.Notes).HasMaxLength(500);
        builder.Property(a => a.CancellationReason).HasMaxLength(300);

        // Optimistic concurrency for status transitions (cancel vs. confirm racing each other).
        builder.Property(a => a.RowVersion).IsRowVersion();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(a => a.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Doctor>().WithMany().HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Patient>().WithMany().HasForeignKey(a => a.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Clinic>().WithMany().HasForeignKey(a => a.ClinicId).OnDelete(DeleteBehavior.Restrict);

        // --- Double-booking protection enforced by SQL Server itself ---
        // Only "slot-occupying" rows participate, so cancelling/rejecting releases the slot
        // while keeping the historical row.
        builder.HasIndex(a => new { a.TenantId, a.DoctorId, a.StartTimeUtc }, AppointmentIndexNames.DoctorSlotUnique)
            .IsUnique()
            .HasFilter(AppointmentIndexNames.ActiveSlotFilter);

        builder.HasIndex(a => new { a.TenantId, a.PatientId, a.StartTimeUtc }, AppointmentIndexNames.PatientSlotUnique)
            .IsUnique()
            .HasFilter(AppointmentIndexNames.ActiveSlotFilter);

        // --- Read-path covering indexes (include cancelled history for schedules/reports) ---
        builder.HasIndex(a => new { a.TenantId, a.DoctorId, a.StartTimeUtc }, AppointmentIndexNames.DoctorDate)
            .IncludeProperties(a => new { a.EndTimeUtc, a.Status });

        builder.HasIndex(a => new { a.TenantId, a.PatientId, a.StartTimeUtc }, AppointmentIndexNames.PatientDate)
            .IncludeProperties(a => new { a.EndTimeUtc, a.DoctorId, a.ClinicId, a.Status });

        builder.HasIndex(a => a.ClinicId).HasDatabaseName("IX_Appointments_ClinicId");

        builder.Ignore(a => a.ScheduledRange);
        builder.Ignore(a => a.IsSlotOccupied);
        builder.Ignore(a => a.DomainEvents);
        builder.ConfigureAudit();
    }
}

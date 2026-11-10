using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBooking.Infrastructure.Persistence.Configurations;

internal sealed class ConsultationNoteConfiguration : IEntityTypeConfiguration<ConsultationNote>
{
    public void Configure(EntityTypeBuilder<ConsultationNote> builder)
    {
        builder.ToTable("ConsultationNotes");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.ChiefComplaint).HasMaxLength(1000);
        builder.Property(c => c.ExaminationNotes).HasMaxLength(3000);
        builder.Property(c => c.Diagnosis).HasMaxLength(1000).IsRequired();
        builder.Property(c => c.TreatmentPlan).HasMaxLength(3000);
        builder.Property(c => c.LabRequests).HasMaxLength(4000);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Appointment>().WithMany().HasForeignKey(c => c.AppointmentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Patient>().WithMany().HasForeignKey(c => c.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Doctor>().WithMany().HasForeignKey(c => c.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Clinic>().WithMany().HasForeignKey(c => c.ClinicId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.TenantId, c.PatientId }).HasDatabaseName("IX_ConsultationNotes_Tenant_Patient");
        builder.HasIndex(c => c.AppointmentId).IsUnique().HasDatabaseName("UX_ConsultationNotes_AppointmentId");

        builder.ConfigureAudit();
        builder.Ignore(c => c.DomainEvents);
    }
}

internal sealed class PatientVitalSignsConfiguration : IEntityTypeConfiguration<PatientVitalSigns>
{
    public void Configure(EntityTypeBuilder<PatientVitalSigns> builder)
    {
        builder.ToTable("PatientVitalSigns");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.TemperatureCelsius).HasPrecision(4, 1);
        builder.Property(v => v.WeightKg).HasPrecision(5, 2);
        builder.Property(v => v.HeightCm).HasPrecision(5, 1);
        builder.Property(v => v.BloodGlucoseMgDl).HasPrecision(5, 1);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(v => v.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Patient>().WithMany().HasForeignKey(v => v.PatientId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(v => new { v.TenantId, v.PatientId }).HasDatabaseName("IX_PatientVitalSigns_Tenant_Patient");

        builder.ConfigureAudit();
        builder.Ignore(v => v.Bmi);
    }
}

internal sealed class PrescriptionConfiguration : IEntityTypeConfiguration<Prescription>
{
    public void Configure(EntityTypeBuilder<Prescription> builder)
    {
        builder.ToTable("Prescriptions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.PrescriptionNumber).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(p => p.Diagnosis).HasMaxLength(1000).IsRequired();
        builder.Property(p => p.GeneralInstructions).HasMaxLength(2000);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Patient>().WithMany().HasForeignKey(p => p.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Doctor>().WithMany().HasForeignKey(p => p.DoctorId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Clinic>().WithMany().HasForeignKey(p => p.ClinicId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => new { p.TenantId, p.PrescriptionNumber }).IsUnique().HasDatabaseName("UX_Prescriptions_Tenant_Number");
        builder.HasIndex(p => new { p.TenantId, p.PatientId }).HasDatabaseName("IX_Prescriptions_Tenant_Patient");

        builder.HasMany(p => p.Items).WithOne().HasForeignKey(i => i.PrescriptionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(p => p.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.ConfigureAudit();
        builder.Ignore(p => p.DomainEvents);
    }
}

internal sealed class PrescriptionItemConfiguration : IEntityTypeConfiguration<PrescriptionItem>
{
    public void Configure(EntityTypeBuilder<PrescriptionItem> builder)
    {
        builder.ToTable("PrescriptionItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.MedicationName).HasMaxLength(200).IsRequired();
        builder.Property(i => i.Dosage).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Frequency).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Duration).HasMaxLength(100).IsRequired();
        builder.Property(i => i.Instructions).HasMaxLength(500);

        builder.HasIndex(i => i.PrescriptionId).HasDatabaseName("IX_PrescriptionItems_PrescriptionId");
    }
}

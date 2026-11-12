using ClinicBooking.Domain.Entities;
using ClinicBooking.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBooking.Infrastructure.Persistence.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.NameEn).HasMaxLength(150).IsRequired();
        builder.Property(t => t.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(t => t.Identifier).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.HasIndex(t => t.Identifier).IsUnique().HasDatabaseName("UX_Tenants_Identifier");

        builder.Property(t => t.SubscriptionStartDateUtc).IsRequired(false);
        builder.Property(t => t.SubscriptionEndDateUtc).IsRequired(false);
        builder.Property(t => t.SubscriptionPlan).HasMaxLength(50).IsRequired(false);

        builder.ConfigureAudit();
        builder.Ignore(t => t.DomainEvents);
    }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.Email)
            .HasConversion(email => email.Value, value => EmailAddress.Create(value))
            .HasMaxLength(256)
            .IsRequired();

        // Emails are globally unique: a person logs in once and the token carries their tenant.
        builder.HasIndex(u => u.Email).IsUnique().HasDatabaseName("UX_Users_Email");

        builder.Property(u => u.FirstName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.LastName).HasMaxLength(50).IsRequired();
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsUnicode(false).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(u => u.ClinicId).IsRequired(false);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(u => u.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(u => u.TenantId).HasDatabaseName("IX_Users_TenantId");
        builder.HasIndex(u => u.ClinicId).HasDatabaseName("IX_Users_ClinicId");

        builder.HasMany(u => u.RefreshTokens)
            .WithOne()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(u => u.RefreshTokens).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(u => u.Permissions)
            .WithOne()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(u => u.Permissions).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.ConfigureAudit();
        builder.Ignore(u => u.DomainEvents);
        builder.Ignore(u => u.FullName);
    }
}

internal sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> builder)
    {
        builder.ToTable("UserPermissions");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Permission).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.HasIndex(p => new { p.UserId, p.Permission }).IsUnique().HasDatabaseName("UX_UserPermissions_User_Permission");
        builder.HasIndex(p => p.TenantId).HasDatabaseName("IX_UserPermissions_TenantId");

        builder.Property(p => p.GrantedAtUtc).HasColumnType("datetime2").IsRequired();
    }
}

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Token).HasMaxLength(200).IsUnicode(false).IsRequired();
        builder.HasIndex(t => t.Token).IsUnique().HasDatabaseName("UX_RefreshTokens_Token");

        builder.Property(t => t.JwtId).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(t => t.CreatedByIp).HasMaxLength(45).IsUnicode(false);
        builder.Property(t => t.RevokedByIp).HasMaxLength(45).IsUnicode(false);
        builder.Property(t => t.ReplacedByToken).HasMaxLength(200).IsUnicode(false);
        builder.Property(t => t.ReasonRevoked).HasMaxLength(200);

        builder.Ignore(t => t.IsActive);
        builder.Ignore(t => t.IsExpired);
        builder.Ignore(t => t.IsRevoked);
    }
}

internal sealed class SpecialtyConfiguration : IEntityTypeConfiguration<Specialty>
{
    public void Configure(EntityTypeBuilder<Specialty> builder)
    {
        builder.ToTable("Specialties");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        // nvarchar (Unicode) columns store Arabic script safely.
        builder.Property(s => s.NameEn).HasMaxLength(100).IsRequired();
        builder.Property(s => s.NameAr).HasMaxLength(100).IsRequired();
        builder.Property(s => s.DescriptionEn).HasMaxLength(500).IsRequired();
        builder.Property(s => s.DescriptionAr).HasMaxLength(500).IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(s => s.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => new { s.TenantId, s.NameEn }).IsUnique().HasDatabaseName("UX_Specialties_Tenant_NameEn");

        builder.ConfigureAudit();
    }
}

internal sealed class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.ToTable("Clinics");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.NameEn).HasMaxLength(150).IsRequired();
        builder.Property(c => c.NameAr).HasMaxLength(150).IsRequired();
        builder.Property(c => c.AddressEn).HasMaxLength(200).IsRequired();
        builder.Property(c => c.AddressAr).HasMaxLength(200).IsRequired();
        builder.Property(c => c.CityEn).HasMaxLength(100).IsRequired();
        builder.Property(c => c.CityAr).HasMaxLength(100).IsRequired();
        builder.Property(c => c.PhoneNumber)
            .HasConversion(p => p.Value, value => PhoneNumber.Create(value))
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(c => c.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(c => c.TenantId).HasDatabaseName("IX_Clinics_TenantId");

        builder.Property(c => c.SubscriptionStartDateUtc).IsRequired(false);
        builder.Property(c => c.SubscriptionEndDateUtc).IsRequired(false);
        builder.Property(c => c.SubscriptionPlan).HasMaxLength(50).IsRequired(false);

        builder.ConfigureAudit();
    }
}

internal sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Gender).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(p => p.BloodGroup).HasMaxLength(10);
        builder.Property(p => p.Allergies).HasMaxLength(500);
        builder.Property(p => p.ChronicConditions).HasMaxLength(500);
        builder.Property(p => p.MedicalNotes).HasMaxLength(1000);
        builder.Property(p => p.EmergencyContact)
            .HasConversion(p => p!.Value, value => PhoneNumber.Create(value))
            .HasMaxLength(20)
            .IsUnicode(false);

        builder.HasOne<Tenant>().WithMany().HasForeignKey(p => p.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithOne().HasForeignKey<Patient>(p => p.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(p => p.UserId).IsUnique().HasDatabaseName("UX_Patients_UserId");
        builder.HasIndex(p => p.TenantId).HasDatabaseName("IX_Patients_TenantId");

        builder.ConfigureAudit();
        builder.Ignore(p => p.DomainEvents);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.EntityName).HasMaxLength(100).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(a => a.Action).HasMaxLength(50).IsRequired();
        builder.Property(a => a.ChangedBy).HasMaxLength(100).IsRequired();
        builder.Property(a => a.ChangedAtUtc).IsRequired();
        builder.Property(a => a.ChangesJson).HasMaxLength(8000);

        builder.HasIndex(a => new { a.TenantId, a.EntityName }).HasDatabaseName("IX_AuditLogs_Tenant_Entity");
        builder.HasIndex(a => a.ChangedAtUtc).HasDatabaseName("IX_AuditLogs_ChangedAtUtc");
    }
}

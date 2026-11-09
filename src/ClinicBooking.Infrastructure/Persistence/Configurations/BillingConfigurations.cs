using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBooking.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.InvoiceNumber).HasMaxLength(50).IsUnicode(false).IsRequired();
        builder.Property(i => i.SubTotal).HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.DiscountAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.TaxAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.PaidAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(i => i.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(i => i.PaymentMethod).HasConversion<string>().HasMaxLength(20).IsRequired();

        builder.HasOne<Tenant>().WithMany().HasForeignKey(i => i.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Appointment>().WithMany().HasForeignKey(i => i.AppointmentId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne<Patient>().WithMany().HasForeignKey(i => i.PatientId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Clinic>().WithMany().HasForeignKey(i => i.ClinicId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.TenantId, i.InvoiceNumber }).IsUnique().HasDatabaseName("UX_Invoices_Tenant_Number");
        builder.HasIndex(i => new { i.TenantId, i.ClinicId }).HasDatabaseName("IX_Invoices_Tenant_Clinic");
        builder.HasIndex(i => new { i.TenantId, i.PatientId }).HasDatabaseName("IX_Invoices_Tenant_Patient");

        builder.HasMany(i => i.Items).WithOne().HasForeignKey(item => item.InvoiceId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(i => i.Items).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.ConfigureAudit();
        builder.Ignore(i => i.DomainEvents);
    }
}

internal sealed class InvoiceItemConfiguration : IEntityTypeConfiguration<InvoiceItem>
{
    public void Configure(EntityTypeBuilder<InvoiceItem> builder)
    {
        builder.ToTable("InvoiceItems");
        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).ValueGeneratedNever();

        builder.Property(i => i.Description).HasMaxLength(250).IsRequired();
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2).IsRequired();

        builder.HasIndex(i => i.InvoiceId).HasDatabaseName("IX_InvoiceItems_InvoiceId");
        builder.Ignore(i => i.TotalPrice);
    }
}

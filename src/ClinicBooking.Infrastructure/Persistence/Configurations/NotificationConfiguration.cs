using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBooking.Infrastructure.Persistence.Configurations;

internal sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> builder)
    {
        builder.ToTable("NotificationLogs");
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedNever();

        builder.Property(n => n.RecipientName).HasMaxLength(200).IsRequired();
        builder.Property(n => n.RecipientPhone).HasMaxLength(50);
        builder.Property(n => n.RecipientEmail).HasMaxLength(256);
        builder.Property(n => n.Channel).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(n => n.Type).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(n => n.Status).HasConversion<string>().HasMaxLength(30).IsRequired();
        builder.Property(n => n.Subject).HasMaxLength(250);
        builder.Property(n => n.Message).HasMaxLength(2000).IsRequired();
        builder.Property(n => n.ErrorMessage).HasMaxLength(1000);
        builder.Property(n => n.SentAtUtc).IsRequired();

        builder.HasIndex(n => n.TenantId).HasDatabaseName("IX_NotificationLogs_TenantId");
        builder.HasIndex(n => n.AppointmentId).HasDatabaseName("IX_NotificationLogs_AppointmentId");
        builder.HasIndex(n => n.SentAtUtc).HasDatabaseName("IX_NotificationLogs_SentAtUtc");
    }
}

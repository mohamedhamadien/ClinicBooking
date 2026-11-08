using ClinicBooking.Application.Common.Exceptions;
using ClinicBooking.Application.Contracts.Services;
using ClinicBooking.Domain.Common;
using ClinicBooking.Domain.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ClinicBooking.Infrastructure.Persistence;

public sealed class ClinicBookingDbContext : DbContext
{
    // SQL Server error numbers for unique constraint / unique index violations.
    private const int SqlUniqueIndexViolation = 2601;
    private const int SqlUniqueConstraintViolation = 2627;

    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public ClinicBookingDbContext(
        DbContextOptions<ClinicBookingDbContext> options,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        ICurrentUserService? currentUserService = null)
        : base(options)
    {
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _currentUserService = currentUserService;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Specialty> Specialties => Set<Specialty>();
    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Doctor> Doctors => Set<Doctor>();
    public DbSet<DoctorClinic> DoctorClinics => Set<DoctorClinic>();
    public DbSet<DoctorAvailability> DoctorAvailabilities => Set<DoctorAvailability>();
    public DbSet<AvailabilityException> AvailabilityExceptions => Set<AvailabilityException>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();

    // Referenced from the global query filters. EF Core evaluates context members per query,
    // so each request sees only its own tenant even though the model is cached.
    internal Guid? CurrentTenantId => _tenantContext.TenantId;
    internal bool IsHostContext => _tenantContext.IsHost;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ClinicBookingDbContext).Assembly);
        ApplyTenantQueryFilters(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // All timestamps are stored as UTC in datetime2 and re-materialized with DateTimeKind.Utc.
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>().HaveColumnType("datetime2");
        configurationBuilder.Properties<DateTime?>().HaveConversion<NullableUtcDateTimeConverter>().HaveColumnType("datetime2");
    }


    private static readonly object _inMemoryLock = new();
    private static readonly Domain.Enums.AppointmentStatus[] _inactiveStatuses =
    [
        Domain.Enums.AppointmentStatus.CancelledByPatient,
        Domain.Enums.AppointmentStatus.CancelledByDoctor,
        Domain.Enums.AppointmentStatus.CancelledByAdmin,
        Domain.Enums.AppointmentStatus.Rejected
    ];

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnforceTenantWriteIsolation();
        ApplyAuditInformation();

        if (Database.ProviderName?.Contains("InMemory") == true)
        {
            lock (_inMemoryLock)
            {
                EnforceInMemoryUniqueConstraints();
                var r = base.SaveChangesAsync(cancellationToken).GetAwaiter().GetResult();
                ClearDomainEvents();
                return r;
            }
        }

        try
        {
            var result = await base.SaveChangesAsync(cancellationToken);
            ClearDomainEvents();
            return result;
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                "Persistence.ConcurrencyConflict",
                "The record was modified by another request. Reload and try again.",
                ex);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sql
                                           && sql.Number is SqlUniqueIndexViolation or SqlUniqueConstraintViolation)
        {
            throw TranslateUniqueViolation(sql.Message, ex);
        }
    }

    private void EnforceInMemoryUniqueConstraints()
    {
        var addedAppointments = ChangeTracker.Entries<Appointment>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified)
            .Select(e => e.Entity)
            .Where(a => !_inactiveStatuses.Contains(a.Status))
            .ToList();

        if (addedAppointments.Count == 0)
        {
            return;
        }

        foreach (var appt in addedAppointments)
        {
            var doctorConflict = Appointments
                .IgnoreQueryFilters()
                .Any(a => a.Id != appt.Id &&
                          a.TenantId == appt.TenantId &&
                          a.DoctorId == appt.DoctorId &&
                          a.StartTimeUtc == appt.StartTimeUtc &&
                          !_inactiveStatuses.Contains(a.Status));

            if (doctorConflict)
            {
                throw new ConcurrencyConflictException(
                    "Appointment.DoctorSlotUnavailable",
                    "The requested appointment slot is no longer available for this doctor.");
            }

            var patientConflict = Appointments
                .IgnoreQueryFilters()
                .Any(a => a.Id != appt.Id &&
                          a.TenantId == appt.TenantId &&
                          a.PatientId == appt.PatientId &&
                          a.StartTimeUtc == appt.StartTimeUtc &&
                          !_inactiveStatuses.Contains(a.Status));

            if (patientConflict)
            {
                throw new ConcurrencyConflictException(
                    "Appointment.PatientDoubleBooking",
                    "The patient already has an active appointment starting at this time.");
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess) =>
        throw new NotSupportedException("Use SaveChangesAsync so tenant isolation, auditing and conflict translation run.");

    private static ConcurrencyConflictException TranslateUniqueViolation(string sqlMessage, Exception inner)
    {
        if (sqlMessage.Contains(AppointmentIndexNames.DoctorSlotUnique, StringComparison.OrdinalIgnoreCase))
        {
            return new ConcurrencyConflictException(
                "Appointment.DoctorSlotUnavailable",
                "The requested appointment slot is no longer available for this doctor.",
                inner);
        }

        if (sqlMessage.Contains(AppointmentIndexNames.PatientSlotUnique, StringComparison.OrdinalIgnoreCase))
        {
            return new ConcurrencyConflictException(
                "Appointment.PatientDoubleBooking",
                "The patient already has an active appointment starting at this time.",
                inner);
        }

        return new ConcurrencyConflictException(
            "Persistence.UniqueConstraintViolation",
            "A record with the same unique values already exists.",
            inner);
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;
            if (entityType.IsOwned() || entityType.BaseType is not null)
            {
                continue;
            }

            if (typeof(IMustHaveTenant).IsAssignableFrom(clrType))
            {
                typeof(ClinicBookingDbContext)
                    .GetMethod(nameof(ApplyMustHaveTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(clrType)
                    .Invoke(this, [modelBuilder]);
            }
            else if (typeof(IMayHaveTenant).IsAssignableFrom(clrType))
            {
                typeof(ClinicBookingDbContext)
                    .GetMethod(nameof(ApplyMayHaveTenantFilter), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                    .MakeGenericMethod(clrType)
                    .Invoke(this, [modelBuilder]);
            }
        }
    }

    // Tenant-owned rows: visible only to their tenant (or the host/platform context).
    // If no tenant is resolved and the caller is not host, nothing is visible (deny by default).
    private void ApplyMustHaveTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IMustHaveTenant
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            IsHostContext || EF.Property<Guid>(e, nameof(IMustHaveTenant.TenantId)) == CurrentTenantId);
    }

    // Optionally tenant-owned rows: shared (TenantId = NULL) rows are visible to everyone,
    // tenant-specific rows only to their tenant (or host).
    private void ApplyMayHaveTenantFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IMayHaveTenant
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e =>
            IsHostContext
            || EF.Property<Guid?>(e, nameof(IMayHaveTenant.TenantId)) == null
            || EF.Property<Guid?>(e, nameof(IMayHaveTenant.TenantId)) == CurrentTenantId);
    }

    /// <summary>
    /// Defense in depth: a request scoped to tenant A can never insert or modify rows of tenant B,
    /// even if a handler has a bug and passes the wrong TenantId.
    /// </summary>
    private void EnforceTenantWriteIsolation()
    {
        if (IsHostContext || CurrentTenantId is null)
        {
            return;
        }

        var isSystemAdmin = string.Equals(_currentUserService?.Role, "Admin", StringComparison.OrdinalIgnoreCase);
        var currentTenantId = CurrentTenantId.Value;

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
            {
                continue;
            }

            var entityTenantId = entry.Entity switch
            {
                IMustHaveTenant mustHave => mustHave.TenantId,
                IMayHaveTenant mayHave => mayHave.TenantId,
                _ => (Guid?)null
            };

            var isSharedRowModification = entry.Entity is IMayHaveTenant && entityTenantId is null;

            // System administrators can create or manage shared global rows (e.g. global Specialties)
            if (isSharedRowModification && isSystemAdmin)
            {
                continue;
            }

            if ((entityTenantId is not null && entityTenantId != currentTenantId) || isSharedRowModification)
            {
                throw new UnauthorizedAccessException(
                    $"Cross-tenant write blocked: {entry.Metadata.ClrType.Name} does not belong to the current tenant.");
            }
        }
    }

    private void ApplyAuditInformation()
    {
        var utcNow = _dateTimeProvider.UtcNow;
        var actor = _currentUserService?.UserId?.ToString() ?? "system";

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = utcNow;
                    entry.Entity.CreatedBy = actor;
                    break;
                case EntityState.Modified:
                    entry.Property(e => e.CreatedAtUtc).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;
                    entry.Entity.LastModifiedAtUtc = utcNow;
                    entry.Entity.LastModifiedBy = actor;
                    break;
            }
        }
    }

    private void ClearDomainEvents()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            switch (entry.Entity)
            {
                case AggregateRoot<Guid> aggregate:
                    aggregate.ClearDomainEvents();
                    break;
            }
        }
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

    private sealed class NullableUtcDateTimeConverter() : ValueConverter<DateTime?, DateTime?>(
        v => v.HasValue ? (v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime()) : v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);
}

public static class AppointmentIndexNames
{
    public const string DoctorSlotUnique = "UX_Appointments_Doctor_Slot";
    public const string PatientSlotUnique = "UX_Appointments_Patient_Slot";
    public const string DoctorDate = "IX_Appointments_Doctor_Date";
    public const string PatientDate = "IX_Appointments_Patient_Date";

    /// <summary>
    /// Statuses that release a slot. Must stay in sync with <see cref="Appointment.IsSlotOccupied"/>.
    /// </summary>
    public const string ActiveSlotFilter =
        "[Status] NOT IN ('CancelledByPatient', 'CancelledByDoctor', 'CancelledByAdmin', 'Rejected')";
}

using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

internal sealed class AuditLogRepository : IAuditLogRepository
{
    private readonly ClinicBookingDbContext _dbContext;

    public AuditLogRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AuditLog>> ListAsync(
        Guid? tenantId,
        string? entityName = null,
        int limit = 100,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AuditLogs.AsNoTracking();

        if (tenantId.HasValue && tenantId.Value != Guid.Empty)
        {
            query = query.Where(a => a.TenantId == tenantId.Value || a.TenantId == null);
        }

        if (!string.IsNullOrWhiteSpace(entityName))
        {
            query = query.Where(a => a.EntityName == entityName);
        }

        return await query
            .OrderByDescending(a => a.ChangedAtUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}

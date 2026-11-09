using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly ClinicBookingDbContext _dbContext;

    public InvoiceRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Invoice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public Task<Invoice?> GetByAppointmentIdAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Invoices
            .Include(i => i.Items)
            .FirstOrDefaultAsync(i => i.AppointmentId == appointmentId, cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetByTenantIdAsync(Guid tenantId, Guid? clinicId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Invoices
            .Include(i => i.Items)
            .Where(i => i.TenantId == tenantId);

        if (clinicId.HasValue)
        {
            query = query.Where(i => i.ClinicId == clinicId.Value);
        }

        return await query
            .OrderByDescending(i => i.IssuedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Invoice>> GetByPatientIdAsync(Guid tenantId, Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Invoices
            .Include(i => i.Items)
            .Where(i => i.TenantId == tenantId && i.PatientId == patientId)
            .OrderByDescending(i => i.IssuedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        await _dbContext.Invoices.AddAsync(invoice, cancellationToken);
    }

    public Task UpdateAsync(Invoice invoice, CancellationToken cancellationToken = default)
    {
        _dbContext.Invoices.Update(invoice);
        return Task.CompletedTask;
    }
}

using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

public sealed class MedicalInventoryRepository : IMedicalInventoryRepository
{
    private readonly ClinicBookingDbContext _dbContext;

    public MedicalInventoryRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<MedicalInventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.MedicalInventoryItems
            .Include(i => i.Clinic)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MedicalInventoryItem>> GetByClinicIdAsync(Guid clinicId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MedicalInventoryItems
            .Include(i => i.Clinic)
            .Where(i => i.ClinicId == clinicId)
            .OrderBy(i => i.ItemName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MedicalInventoryItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.MedicalInventoryItems
            .Include(i => i.Clinic)
            .OrderBy(i => i.ItemName)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MedicalInventoryItem item, CancellationToken cancellationToken = default)
    {
        await _dbContext.MedicalInventoryItems.AddAsync(item, cancellationToken);
    }

    public void Delete(MedicalInventoryItem item)
    {
        _dbContext.MedicalInventoryItems.Remove(item);
    }
}

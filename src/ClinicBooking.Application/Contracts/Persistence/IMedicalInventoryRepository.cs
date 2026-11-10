using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Contracts.Persistence;

public interface IMedicalInventoryRepository
{
    Task<MedicalInventoryItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicalInventoryItem>> GetByClinicIdAsync(Guid clinicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicalInventoryItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(MedicalInventoryItem item, CancellationToken cancellationToken = default);
    void Delete(MedicalInventoryItem item);
}

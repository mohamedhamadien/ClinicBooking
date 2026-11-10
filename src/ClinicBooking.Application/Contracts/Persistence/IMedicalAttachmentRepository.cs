using ClinicBooking.Domain.Entities;

namespace ClinicBooking.Application.Contracts.Persistence;

public interface IMedicalAttachmentRepository
{
    Task<MedicalAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MedicalAttachment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task AddAsync(MedicalAttachment attachment, CancellationToken cancellationToken = default);
    void Delete(MedicalAttachment attachment);
}

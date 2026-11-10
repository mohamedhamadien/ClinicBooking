using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Infrastructure.Persistence.Repositories;

public sealed class MedicalAttachmentRepository : IMedicalAttachmentRepository
{
    private readonly ClinicBookingDbContext _dbContext;

    public MedicalAttachmentRepository(ClinicBookingDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<MedicalAttachment?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.MedicalAttachments.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MedicalAttachment>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.MedicalAttachments
            .Where(a => a.PatientId == patientId)
            .OrderByDescending(a => a.UploadedAtUtc)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(MedicalAttachment attachment, CancellationToken cancellationToken = default)
    {
        await _dbContext.MedicalAttachments.AddAsync(attachment, cancellationToken);
    }

    public void Delete(MedicalAttachment attachment)
    {
        _dbContext.MedicalAttachments.Remove(attachment);
    }
}

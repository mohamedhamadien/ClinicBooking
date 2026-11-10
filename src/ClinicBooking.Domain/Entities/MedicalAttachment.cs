using ClinicBooking.Domain.Common;

namespace ClinicBooking.Domain.Entities;

public sealed class MedicalAttachment : Entity<Guid>, IMayHaveTenant, ISoftDeletable
{
    private MedicalAttachment() { }

    public Guid PatientId { get; private set; }
    public Patient? Patient { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string AttachmentType { get; private set; } = "LabResult"; // LabResult, XRay, MRI, Ultrasound, PrescriptionScan, Report, Other
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = "application/octet-stream";
    public long FileSizeBytes { get; private set; }
    public string DataUrl { get; private set; } = string.Empty; // Base64 data URL for instant viewing
    public string? Notes { get; private set; }
    public DateTime UploadedAtUtc { get; private set; }
    public Guid? TenantId { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    public static MedicalAttachment Create(
        Guid patientId,
        string title,
        string attachmentType,
        string fileName,
        string contentType,
        long fileSizeBytes,
        string dataUrl,
        string? notes,
        Guid? tenantId = null)
    {
        return new MedicalAttachment
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Title = title,
            AttachmentType = string.IsNullOrWhiteSpace(attachmentType) ? "LabResult" : attachmentType,
            FileName = fileName,
            ContentType = contentType,
            FileSizeBytes = fileSizeBytes,
            DataUrl = dataUrl,
            Notes = notes,
            UploadedAtUtc = DateTime.UtcNow,
            TenantId = tenantId
        };
    }
}

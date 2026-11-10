namespace ClinicBooking.Application.Features.MedicalAttachments.DTOs;

public sealed record MedicalAttachmentDto(
    Guid Id,
    Guid PatientId,
    string Title,
    string AttachmentType,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string DataUrl,
    string? Notes,
    DateTime UploadedAtUtc);

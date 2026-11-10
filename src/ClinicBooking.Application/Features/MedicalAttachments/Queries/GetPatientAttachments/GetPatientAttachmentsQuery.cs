using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.MedicalAttachments.DTOs;
using MediatR;

namespace ClinicBooking.Application.Features.MedicalAttachments.Queries.GetPatientAttachments;

public sealed record GetPatientAttachmentsQuery(Guid PatientId) : IRequest<Result<IReadOnlyList<MedicalAttachmentDto>>>;

public sealed class GetPatientAttachmentsQueryHandler : IRequestHandler<GetPatientAttachmentsQuery, Result<IReadOnlyList<MedicalAttachmentDto>>>
{
    private readonly IMedicalAttachmentRepository _attachmentRepository;

    public GetPatientAttachmentsQueryHandler(IMedicalAttachmentRepository attachmentRepository)
    {
        _attachmentRepository = attachmentRepository;
    }

    public async Task<Result<IReadOnlyList<MedicalAttachmentDto>>> Handle(GetPatientAttachmentsQuery request, CancellationToken cancellationToken)
    {
        var list = await _attachmentRepository.GetByPatientIdAsync(request.PatientId, cancellationToken);
        var dtos = list.Select(a => new MedicalAttachmentDto(
            a.Id,
            a.PatientId,
            a.Title,
            a.AttachmentType,
            a.FileName,
            a.ContentType,
            a.FileSizeBytes,
            a.DataUrl,
            a.Notes,
            a.UploadedAtUtc)).ToList();

        return Result.Success<IReadOnlyList<MedicalAttachmentDto>>(dtos);
    }
}

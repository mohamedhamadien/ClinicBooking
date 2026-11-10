using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using ClinicBooking.Application.Features.MedicalAttachments.DTOs;
using ClinicBooking.Domain.Entities;
using FluentValidation;
using MediatR;

namespace ClinicBooking.Application.Features.MedicalAttachments.Commands.UploadMedicalAttachment;

public sealed record UploadMedicalAttachmentCommand(
    Guid PatientId,
    string Title,
    string AttachmentType,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string DataUrl,
    string? Notes = null,
    Guid? TenantId = null) : IRequest<Result<MedicalAttachmentDto>>;

public sealed class UploadMedicalAttachmentCommandValidator : AbstractValidator<UploadMedicalAttachmentCommand>
{
    public UploadMedicalAttachmentCommandValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.DataUrl).NotEmpty();
    }
}

public sealed class UploadMedicalAttachmentCommandHandler : IRequestHandler<UploadMedicalAttachmentCommand, Result<MedicalAttachmentDto>>
{
    private readonly IMedicalAttachmentRepository _attachmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UploadMedicalAttachmentCommandHandler(
        IMedicalAttachmentRepository attachmentRepository,
        IUnitOfWork unitOfWork)
    {
        _attachmentRepository = attachmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<MedicalAttachmentDto>> Handle(UploadMedicalAttachmentCommand request, CancellationToken cancellationToken)
    {
        var attachment = MedicalAttachment.Create(
            request.PatientId,
            request.Title,
            request.AttachmentType,
            request.FileName,
            request.ContentType,
            request.FileSizeBytes,
            request.DataUrl,
            request.Notes,
            request.TenantId);

        await _attachmentRepository.AddAsync(attachment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var dto = new MedicalAttachmentDto(
            attachment.Id,
            attachment.PatientId,
            attachment.Title,
            attachment.AttachmentType,
            attachment.FileName,
            attachment.ContentType,
            attachment.FileSizeBytes,
            attachment.DataUrl,
            attachment.Notes,
            attachment.UploadedAtUtc);

        return Result.Success(dto);
    }
}

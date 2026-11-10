using ClinicBooking.Application.Common.Results;
using ClinicBooking.Application.Contracts.Persistence;
using MediatR;

namespace ClinicBooking.Application.Features.MedicalAttachments.Commands.DeleteMedicalAttachment;

public sealed record DeleteMedicalAttachmentCommand(Guid Id) : IRequest<Result<Unit>>;

public sealed class DeleteMedicalAttachmentCommandHandler : IRequestHandler<DeleteMedicalAttachmentCommand, Result<Unit>>
{
    private readonly IMedicalAttachmentRepository _attachmentRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteMedicalAttachmentCommandHandler(
        IMedicalAttachmentRepository attachmentRepository,
        IUnitOfWork unitOfWork)
    {
        _attachmentRepository = attachmentRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Unit>> Handle(DeleteMedicalAttachmentCommand request, CancellationToken cancellationToken)
    {
        var attachment = await _attachmentRepository.GetByIdAsync(request.Id, cancellationToken);
        if (attachment is null)
        {
            return Result.Failure<Unit>(Error.NotFound("MedicalAttachment.NotFound", "The requested medical document was not found."));
        }

        _attachmentRepository.Delete(attachment);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Unit.Value);
    }
}

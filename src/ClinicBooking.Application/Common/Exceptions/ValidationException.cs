using ClinicBooking.Application.Common.Results;

namespace ClinicBooking.Application.Common.Exceptions;

public sealed class ValidationException : Exception
{
    public IReadOnlyCollection<Error> Errors { get; }

    public ValidationException(IReadOnlyCollection<Error> errors)
        : base("One or more validation failures occurred.")
    {
        Errors = errors;
    }
}

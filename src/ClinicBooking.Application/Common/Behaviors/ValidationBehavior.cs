using ClinicBooking.Application.Common.Results;
using FluentValidation;
using MediatR;
using AppValidationException = ClinicBooking.Application.Common.Exceptions.ValidationException;

namespace ClinicBooking.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .Select(f => Error.Validation(f.PropertyName, f.ErrorMessage))
            .Distinct()
            .ToList();

        if (failures.Count != 0)
        {
            // If TResponse is a Result or Result<T>, return failure result rather than throwing
            if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(failures.First());
            }

            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var failureMethod = typeof(Result)
                    .GetMethods()
                    .First(m => m.Name == nameof(Result.Failure) && m.IsGenericMethod)
                    .MakeGenericMethod(typeof(TResponse).GetGenericArguments()[0]);

                return (TResponse)failureMethod.Invoke(null, [failures.First()])!;
            }

            throw new AppValidationException(failures);
        }

        return await next();
    }
}

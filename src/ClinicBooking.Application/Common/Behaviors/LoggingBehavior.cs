using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ClinicBooking.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Processing command/query {RequestName}", requestName);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var response = await next();
            stopwatch.Stop();

            if (stopwatch.ElapsedMilliseconds > 1000)
            {
                _logger.LogWarning("Long-running request {RequestName} took {ElapsedMilliseconds} ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation("Completed request {RequestName} in {ElapsedMilliseconds} ms",
                    requestName, stopwatch.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Request {RequestName} failed after {ElapsedMilliseconds} ms",
                requestName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}

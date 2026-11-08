namespace ClinicBooking.Application.Common.Exceptions;

/// <summary>
/// Raised by the persistence layer when the database rejects a write because of a
/// uniqueness constraint (e.g. the filtered double-booking indexes) or an optimistic
/// concurrency token mismatch. The API layer maps this to HTTP 409 Conflict.
/// </summary>
public sealed class ConcurrencyConflictException : Exception
{
    public string Code { get; }

    public ConcurrencyConflictException(string code, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
    }
}

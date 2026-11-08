namespace ClinicBooking.Application.Common.Results;

public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static readonly Error NullValue = new("Error.NullValue", "The specified result value is null.");

    public static Error NotFound(string entity, object key) =>
        new($"{entity}.NotFound", $"{entity} with key '{key}' was not found.");

    public static Error Validation(string code, string description) =>
        new(code, description);

    public static Error Conflict(string code, string description) =>
        new(code, description);

    public static Error Unauthorized(string description = "You are not authorized to perform this operation.") =>
        new("Auth.Unauthorized", description);

    public static Error Forbidden(string description = "Access to this resource is forbidden.") =>
        new("Auth.Forbidden", description);
}

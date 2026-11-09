using ClinicBooking.Application.Contracts.Services;

namespace ClinicBooking.Application.Tests.TestHelpers;

public class FakePasswordHasher : IPasswordHasher
{
    public string HashPassword(string password) => $"hashed_{password}";

    public bool VerifyPassword(string passwordHash, string providedPassword) =>
        passwordHash == $"hashed_{providedPassword}";
}

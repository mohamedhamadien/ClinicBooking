namespace ClinicBooking.Domain.Tests;

public class ArchitectureSanityTests
{
    [Fact]
    public void DomainAssembly_ShouldBeAvailable()
    {
        var type = typeof(AssemblyReference);
        Assert.NotNull(type);
    }
}

using FhirParametersGenerator.Tests.Helpers;

namespace FhirParametersGenerator.Tests;

[UsesVerify]
public class FhirParametersGeneratorSnapshotTests
{
    [Fact]
    public Task GeneratesFhirParametersExtensionsCorrectly()
    {
        // The source code to test
        var source = @"
using FhirParametersGenerator;

namespace FhirParametersGenerator.Tests;

[GenerateFhirParameters]
public class TestModel
{
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; } = 0;
    public bool IsSomething { get; set; } = false;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public DateTime Time { get; set; } = DateTime.UtcNow;
    public string WriteOnly
    {
        set
        {
            Console.WriteLine($""Write-only: {value}"");
        }
    }
}";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }

    [Fact]
    public Task ModelWithUnsupportedPropertyTypes_ShouldEmitDiagnosticsAndUseToStringDefaultMappingBehavior()
    {
        // The source code to test
        var source = @"
using FhirParametersGenerator;

namespace FhirParametersGenerator.Tests;

[GenerateFhirParameters]
public class TestModel
{
    public DayOfWeek DayOfWeek { get; init; } = DayOfWeek.Friday;
}";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }
}

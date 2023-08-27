using FhirParametersGenerator.Tests.Helpers;

namespace FhirParametersGenerator.Tests;

[UsesVerify]
public class FhirParametersGeneratorSnapshotTests
{
    [Fact]
    public Task GeneratesFhirParametersExtensionsCorrectly()
    {
        // The source code to test
        var source =
            @"
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
        var source =
            @"
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

    [Fact]
    public Task ModelWithFhirBaseDerivedType_ShouldBeMappedToParameterAsIs()
    {
        // The source code to test
        var source =
            @"
using FhirParametersGenerator;
// essential that this is part of the compilation unit
using Hl7.Fhir.Model;

namespace FhirParametersGenerator.Tests;

[GenerateFhirParameters]
public class ModelWithFhirBaseDerivedType
{
    public CodeableConcept Code { get; init; } = new(""http://snomed.info/sct"", ""386661006"", ""Fever"");
    public Patient Patient { get; init; } = new()
    {
        BirthDate = ""2000-01-01"",
        Deceased = new FhirBoolean(false),
        Name = new() { new HumanName() { Given = new[] { ""Test"" }, Family = ""User"" } },
    }
}";

        // Pass the source code to our helper and snapshot test the output
        return TestHelper.Verify(source);
    }
}

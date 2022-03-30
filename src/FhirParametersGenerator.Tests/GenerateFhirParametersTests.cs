using FluentAssertions;
using Hl7.Fhir.Model;

namespace FhirParametersGenerator.Tests;

public class GenerateFhirParametersTests
{
    [GenerateFhirParameters]
    public class SimpleNameAndAgeModel
    {
        public string Name { get; init; } = string.Empty;
        public int Age { get; init; } = 0;
    }

    [Fact]
    public void ModelWithStringAndInt_ShouldBeMappedCorrectly()
    {
        var t = new SimpleNameAndAgeModel
        {
            Name = "Hello",
            Age = 123,
        };

        var asParameters = t.ToFhirParameters();

        asParameters.GetSingleValue<FhirString>("name").Value.Should().Be(t.Name);
        asParameters.GetSingleValue<FhirDecimal>("age").Value.Should().Be(t.Age);
    }

    [GenerateFhirParameters]
    public class PascalCasedModel
    {
        public string ALongPascalCaseProperty { get; init; } = string.Empty;
        public string Id { get; init; } = string.Empty;
    }

    [Fact]
    public void PascalCasePropertyNames_ShouldBeMappedToCamelCase()
    {
        var t = new PascalCasedModel
        {
            ALongPascalCaseProperty = "Hello",
            Id = "123",
        };

        var asParameters = t.ToFhirParameters();

        asParameters.GetSingleValue<FhirString>("aLongPascalCaseProperty").Value.Should().Be(t.ALongPascalCaseProperty);
        asParameters.GetSingleValue<FhirString>("id").Value.Should().Be(t.Id);
    }

    [GenerateFhirParameters]
    public class ModelWithWriteOnlyProperty
    {
        public string Name { get; init; } = string.Empty;
        public string WriteOnly
        {
            set
            { }
        }
    }

    [Fact]
    public void WriteOnlyProperties_ShouldBeIgnored()
    {
        var m = new ModelWithWriteOnlyProperty
        {
            Name = "Hello",
            WriteOnly = "wo",
        };

        var asParameters = m.ToFhirParameters();

        asParameters.GetSingleValue<FhirString>("name").Value.Should().Be(m.Name);
        asParameters.GetSingleValue<FhirDecimal>("wo").Should().BeNull();
    }
}

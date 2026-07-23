using System.Collections.Generic;
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
        var t = new SimpleNameAndAgeModel { Name = "Hello", Age = 123 };

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
        var t = new PascalCasedModel { ALongPascalCaseProperty = "Hello", Id = "123" };

        var asParameters = t.ToFhirParameters();

        asParameters
            .GetSingleValue<FhirString>("aLongPascalCaseProperty")
            .Value.Should()
            .Be(t.ALongPascalCaseProperty);
        asParameters.GetSingleValue<FhirString>("id").Value.Should().Be(t.Id);
    }

    [GenerateFhirParameters]
    public class ModelWithWriteOnlyProperty
    {
        public string Name { get; init; } = string.Empty;
        public string WriteOnly
        {
            set { }
        }
    }

    [Fact]
    public void WriteOnlyProperties_ShouldBeIgnored()
    {
        var m = new ModelWithWriteOnlyProperty { Name = "Hello", WriteOnly = "wo" };

        var asParameters = m.ToFhirParameters();

        asParameters.GetSingleValue<FhirString>("name").Value.Should().Be(m.Name);
        asParameters.GetSingleValue<FhirDecimal>("wo").Should().BeNull();
    }

    // Not annotated — used as the nested type for ModelWithNested below.
    public class NestedData
    {
        public string Key { get; init; } = string.Empty;
        public bool Flag { get; init; }
    }

    [GenerateFhirParameters]
    public class ModelWithNested
    {
        public string Name { get; init; } = string.Empty;
        public NestedData? Data { get; init; }
    }

    [Fact]
    public void NestedComplexType_ShouldBeMappedToParameterComponent()
    {
        var m = new ModelWithNested
        {
            Name = "test",
            Data = new NestedData { Key = "mykey", Flag = true },
        };

        var parameters = m.ToFhirParameters();

        parameters.GetSingleValue<FhirString>("name").Value.Should().Be("test");

        var dataParam = parameters.Parameter.FirstOrDefault(p => p.Name == "data");
        dataParam.Should().NotBeNull();
        ((FhirString)dataParam!.Part.First(p => p.Name == "key").Value).Value.Should().Be("mykey");
        ((FhirBoolean)dataParam.Part.First(p => p.Name == "flag").Value).Value.Should().Be(true);
    }

    [Fact]
    public void NestedComplexType_WhenNull_ShouldBeSkipped()
    {
        var m = new ModelWithNested { Name = "test", Data = null };

        var parameters = m.ToFhirParameters();

        parameters.GetSingleValue<FhirString>("name").Value.Should().Be("test");
        parameters.Parameter.Should().NotContain(p => p.Name == "data");
    }

    [GenerateFhirParameters]
    public class ModelWithStringList
    {
        public List<string> Tags { get; init; } = new();
    }

    [Fact]
    public void ListOfStrings_ShouldBeMappedToMultipleParameters()
    {
        var m = new ModelWithStringList
        {
            Tags = new List<string> { "tag1", "tag2", "tag3" },
        };

        var parameters = m.ToFhirParameters();

        parameters.Parameter.Where(p => p.Name == "tags").Should().HaveCount(3);
        parameters
            .Parameter.Where(p => p.Name == "tags")
            .Select(p => ((FhirString)p.Value).Value)
            .Should()
            .BeEquivalentTo(new[] { "tag1", "tag2", "tag3" });
    }

    [Fact]
    public void ListOfStrings_WhenNull_ShouldBeSkipped()
    {
        var m = new ModelWithStringList { Tags = null! };

        var parameters = m.ToFhirParameters();

        parameters.Parameter.Should().NotContain(p => p.Name == "tags");
    }

    public class RuleConfig
    {
        public string Path { get; init; } = string.Empty;
        public string Method { get; init; } = string.Empty;
    }

    [GenerateFhirParameters]
    public class ModelWithComplexList
    {
        public List<RuleConfig> Rules { get; init; } = new();
    }

    [Fact]
    public void ListOfComplexTypes_ShouldBeMappedToMultipleParameterComponents()
    {
        var m = new ModelWithComplexList
        {
            Rules = new List<RuleConfig>
            {
                new() { Path = "Patient.name", Method = "redact" },
                new() { Path = "Patient.birthDate", Method = "dateShift" },
            },
        };

        var parameters = m.ToFhirParameters();

        parameters.Parameter.Where(p => p.Name == "rules").Should().HaveCount(2);

        var first = parameters.Parameter.First(p => p.Name == "rules");
        ((FhirString)first.Part.First(p => p.Name == "path").Value)
            .Value.Should()
            .Be("Patient.name");
        ((FhirString)first.Part.First(p => p.Name == "method").Value).Value.Should().Be("redact");

        var second = parameters.Parameter.Last(p => p.Name == "rules");
        ((FhirString)second.Part.First(p => p.Name == "path").Value)
            .Value.Should()
            .Be("Patient.birthDate");
        ((FhirString)second.Part.First(p => p.Name == "method").Value)
            .Value.Should()
            .Be("dateShift");
    }

    [Fact]
    public void ListOfComplexTypes_WhenNull_ShouldBeSkipped()
    {
        var m = new ModelWithComplexList { Rules = null! };

        var parameters = m.ToFhirParameters();

        parameters.Parameter.Should().NotContain(p => p.Name == "rules");
    }
}

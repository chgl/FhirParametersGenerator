using System.Collections.Generic;
using FluentAssertions;
using Hl7.Fhir.Model;

namespace FhirParametersGenerator.Tests;

public partial class GenerateFhirParametersTests
{
    [GenerateFhirParameters]
    public partial class SimpleNameAndAgeModel
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

    [Fact]
    public void ModelWithStringAndInt_ShouldRoundTripFromParameters()
    {
        var t = new SimpleNameAndAgeModel { Name = "Hello", Age = 123 };

        var roundTripped = SimpleNameAndAgeModel.FromFhirParameters(t.ToFhirParameters());

        roundTripped.Name.Should().Be(t.Name);
        roundTripped.Age.Should().Be(t.Age);
    }

    [GenerateFhirParameters]
    public partial class PascalCasedModel
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

    [Fact]
    public void PascalCasePropertyNames_ShouldRoundTripFromParameters()
    {
        var t = new PascalCasedModel { ALongPascalCaseProperty = "Hello", Id = "123" };

        var roundTripped = PascalCasedModel.FromFhirParameters(t.ToFhirParameters());

        roundTripped.ALongPascalCaseProperty.Should().Be(t.ALongPascalCaseProperty);
        roundTripped.Id.Should().Be(t.Id);
    }

    [GenerateFhirParameters]
    public partial class ModelWithWriteOnlyProperty
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
    public partial class ModelWithNested
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

    [Fact]
    public void NestedComplexType_ShouldRoundTripFromParameters()
    {
        var m = new ModelWithNested
        {
            Name = "test",
            Data = new NestedData { Key = "mykey", Flag = true },
        };

        var roundTripped = ModelWithNested.FromFhirParameters(m.ToFhirParameters());

        roundTripped.Name.Should().Be("test");
        roundTripped.Data.Should().NotBeNull();
        roundTripped.Data!.Key.Should().Be("mykey");
        roundTripped.Data.Flag.Should().BeTrue();
    }

    [Fact]
    public void NestedComplexType_WhenAbsentFromParameters_ShouldBeNull()
    {
        var m = new ModelWithNested { Name = "test", Data = null };

        var roundTripped = ModelWithNested.FromFhirParameters(m.ToFhirParameters());

        roundTripped.Data.Should().BeNull();
    }

    [GenerateFhirParameters]
    public partial class ModelWithStringList
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

    [Fact]
    public void ListOfStrings_ShouldRoundTripFromParameters()
    {
        var m = new ModelWithStringList
        {
            Tags = new List<string> { "tag1", "tag2", "tag3" },
        };

        var roundTripped = ModelWithStringList.FromFhirParameters(m.ToFhirParameters());

        roundTripped.Tags.Should().BeEquivalentTo(m.Tags);
    }

    public class RuleConfig
    {
        public string Path { get; init; } = string.Empty;
        public string Method { get; init; } = string.Empty;
    }

    [GenerateFhirParameters]
    public partial class ModelWithComplexList
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

    [Fact]
    public void ListOfComplexTypes_ShouldRoundTripFromParameters()
    {
        var m = new ModelWithComplexList
        {
            Rules = new List<RuleConfig>
            {
                new() { Path = "Patient.name", Method = "redact" },
                new() { Path = "Patient.birthDate", Method = "dateShift" },
            },
        };

        var roundTripped = ModelWithComplexList.FromFhirParameters(m.ToFhirParameters());

        roundTripped.Rules.Should().HaveCount(2);
        roundTripped.Rules[0].Path.Should().Be("Patient.name");
        roundTripped.Rules[0].Method.Should().Be("redact");
        roundTripped.Rules[1].Path.Should().Be("Patient.birthDate");
        roundTripped.Rules[1].Method.Should().Be("dateShift");
    }

    [GenerateFhirParameters]
    public partial class ModelWithEnum
    {
        public DayOfWeek Day { get; init; } = DayOfWeek.Monday;
    }

    [Fact]
    public void EnumProperty_ShouldRoundTripFromParameters()
    {
        var m = new ModelWithEnum { Day = DayOfWeek.Saturday };

        var roundTripped = ModelWithEnum.FromFhirParameters(m.ToFhirParameters());

        roundTripped.Day.Should().Be(DayOfWeek.Saturday);
    }

    [GenerateFhirParameters]
    public partial class ModelWithFhirTypes
    {
        public CodeableConcept? Code { get; init; }
        public Patient? Patient { get; init; }
    }

    [Fact]
    public void FhirBaseAndResourceDerivedProperties_ShouldRoundTripFromParameters()
    {
        var m = new ModelWithFhirTypes
        {
            Code = new CodeableConcept("http://snomed.info/sct", "386661006", "Fever"),
            Patient = new Patient { BirthDate = "2000-01-01" },
        };

        var roundTripped = ModelWithFhirTypes.FromFhirParameters(m.ToFhirParameters());

        roundTripped.Code.Should().NotBeNull();
        roundTripped
            .Code!.Coding.Should()
            .ContainSingle(c => c.System == "http://snomed.info/sct" && c.Code == "386661006");
        roundTripped.Patient.Should().NotBeNull();
        roundTripped.Patient!.BirthDate.Should().Be("2000-01-01");
    }

    // Regression test: a property typed exactly `Resource` (not a concrete subtype like Patient)
    // used to fail to compile. `InheritsFrom` only walks strict base types, so
    // `Resource.InheritsFrom("Hl7.Fhir.Model.Resource")` came back false, which made the generator
    // read the value via `.Value as Resource` instead of `.Resource as Resource` - and since
    // `Value` is typed `DataType` (unrelated to `Resource`), that's a compile error (CS0039).
    [GenerateFhirParameters]
    public partial class ModelWithExactResourceType
    {
        public Resource? AnyResource { get; init; }
    }

    [Fact]
    public void ExactlyResourceTypedProperty_ShouldRoundTripFromParameters()
    {
        var m = new ModelWithExactResourceType
        {
            AnyResource = new Patient { BirthDate = "2000-01-01" },
        };

        var roundTripped = ModelWithExactResourceType.FromFhirParameters(m.ToFhirParameters());

        roundTripped.AnyResource.Should().BeOfType<Patient>();
        ((Patient)roundTripped.AnyResource!).BirthDate.Should().Be("2000-01-01");
    }
}

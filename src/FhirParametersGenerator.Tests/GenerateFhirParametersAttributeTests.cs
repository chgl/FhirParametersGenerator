using FluentAssertions;

namespace FhirParametersGenerator.Tests;

public class GenerateFhirParametersAttributeTests
{
    [Fact]
    public void Attribute_ShouldBeConstructableAndShouldBeOfTypeAttribute()
    {
        var attribute = new GenerateFhirParametersAttribute();

        attribute.Should().BeAssignableTo(typeof(Attribute));
    }
}

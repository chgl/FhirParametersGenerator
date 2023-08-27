using FhirParametersGenerator.Tests.Helpers;

namespace FhirParametersGenerator.Tests;

[UsesVerify]
public class FhirParametersGeneratorSnapshotTests
{
    [Fact]
    public Task GeneratesCodeFromOperationDefinitionCorrectly()
    {
        return TestHelper.Verify(
            new[]
            {
                "Fixtures/OperationDefinitions/OperationDefinition-ConceptMap-closure.json",
                "Fixtures/OperationDefinitions/OperationDefinition-Measure-evaluate-measure.json",
                "Fixtures/OperationDefinitions/OperationDefinition-ConceptMap-translate.json",
            }
        );
    }
}

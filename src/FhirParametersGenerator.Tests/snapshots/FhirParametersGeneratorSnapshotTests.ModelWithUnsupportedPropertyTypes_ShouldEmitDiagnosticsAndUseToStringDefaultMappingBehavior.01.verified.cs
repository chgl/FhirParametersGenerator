//HintName: TestModelFhirParametersExtensions.g.cs
using Hl7.Fhir.Model;
// FhirParametersExtensions generated for type 'TestModel'
namespace FhirParametersGenerator.Tests;

public static class TestModelFhirParametersExtensions
{
    public static Parameters AsFhirParameters(this FhirParametersGenerator.Tests.TestModel model)
    {
        var parameters = new Parameters();
        // DayOfWeek (DayOfWeek) FhirParametersGenerator.Tests.TestModel.DayOfWeek
        parameters.Add("dayOfWeek", new FhirString(model.DayOfWeek.ToString()));
        return parameters;

    }
}

//HintName: TestModelFhirParametersExtensions.g.cs
using Hl7.Fhir.Model;

// FhirParametersExtensions generated for type 'TestModel'
namespace FhirParametersGenerator.Tests;

public static class TestModelFhirParametersExtensions
{
    [Obsolete("AsFhirParameters is deprecated, please use ToFhirParameters instead.")]
    public static Parameters AsFhirParameters(this FhirParametersGenerator.Tests.TestModel model)
    {
        return ToFhirParameters(model);
    }

    public static Parameters ToFhirParameters(this FhirParametersGenerator.Tests.TestModel model)
    {
        var parameters = new Parameters();
        // string (string) FhirParametersGenerator.Tests.TestModel.Name
        parameters.Add("name", new FhirString(model.Name));
        // int (int) FhirParametersGenerator.Tests.TestModel.Age
        parameters.Add("age", new FhirDecimal(model.Age));
        // bool (bool) FhirParametersGenerator.Tests.TestModel.IsSomething
        parameters.Add("isSomething", new FhirBoolean(model.IsSomething));
        // DateTimeOffset (DateTimeOffset) FhirParametersGenerator.Tests.TestModel.Timestamp
        parameters.Add("timestamp", new FhirDateTime(model.Timestamp));
        // DateTime (DateTime) FhirParametersGenerator.Tests.TestModel.Time
        parameters.Add("time", new FhirDateTime(model.Time));
        return parameters;
    }
}

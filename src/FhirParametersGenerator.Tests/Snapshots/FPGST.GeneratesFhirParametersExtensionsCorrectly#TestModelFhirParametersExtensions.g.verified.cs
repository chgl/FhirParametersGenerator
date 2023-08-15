//HintName: TestModelFhirParametersExtensions.g.cs
using Hl7.Fhir.Model;
// FhirParametersExtensions generated for type 'TestModel'
namespace FhirParametersGenerator.Tests;

/// <summary>
/// Code-generated extension methods to convert the model class to a FHIR Parameters resources.
/// </summary>
public static class TestModelFhirParametersExtensions
{
    /// <summary>
    /// Convert the model class to its FHIR Parameters representation.
    /// </summary>
    /// <param name="model">The model class.</param>
    /// <returns>A FHIR Parameters instance.</returns>
    [Obsolete("AsFhirParameters is deprecated, please use ToFhirParameters instead.")]
    public static Parameters AsFhirParameters(this FhirParametersGenerator.Tests.TestModel model)
    {
        return ToFhirParameters(model);
    }

    /// <summary>
    /// Convert the model class to its FHIR Parameters representation.
    /// </summary>
    /// <param name="model">The model class.</param>
    /// <returns>A FHIR Parameters instance.</returns>
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

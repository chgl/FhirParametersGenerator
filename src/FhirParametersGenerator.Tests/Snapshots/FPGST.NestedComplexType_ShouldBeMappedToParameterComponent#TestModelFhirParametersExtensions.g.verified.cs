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
        // FhirParametersGenerator.Tests.NestedConfig (FhirParametersGenerator.Tests.NestedConfig) FhirParametersGenerator.Tests.TestModel.Config
        if (model.Config != null)
        {
            var configComponent = new Parameters.ParameterComponent { Name = "config" };
            // string (string) FhirParametersGenerator.Tests.NestedConfig.Key
            configComponent.Part.Add(new Parameters.ParameterComponent { Name = "key", Value = new FhirString(model.Config.Key) });
            // bool (bool) FhirParametersGenerator.Tests.NestedConfig.Enabled
            configComponent.Part.Add(new Parameters.ParameterComponent { Name = "enabled", Value = new FhirBoolean(model.Config.Enabled) });
            parameters.Parameter.Add(configComponent);
        }
        return parameters;

    }
}

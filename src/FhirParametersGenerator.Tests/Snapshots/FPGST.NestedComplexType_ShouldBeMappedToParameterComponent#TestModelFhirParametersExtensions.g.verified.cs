//HintName: TestModelFhirParametersExtensions.g.cs
using Hl7.Fhir.Model;
using System;
using System.Linq;
// FhirParametersExtensions generated for type 'TestModel'
namespace FhirParametersGenerator.Tests;

/// <summary>
/// Code-generated extension methods to convert the model class to a FHIR Parameters resource.
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
partial class TestModel
{
    /// <summary>
    /// Convert a FHIR Parameters resource back to an instance of <see cref="TestModel"/>.
    /// </summary>
    /// <param name="parameters">The FHIR Parameters instance.</param>
    /// <returns>A new instance of <see cref="TestModel"/> populated from the given parameters.</returns>
    public static FhirParametersGenerator.Tests.TestModel FromFhirParameters(Parameters parameters)
    {
        return new FhirParametersGenerator.Tests.TestModel
        {
            Name = (parameters.GetSingle("name")?.Value as FhirString)?.Value ?? string.Empty,
            Config = parameters.GetSingle("config") is { } configComponent ? BuildNestedConfig(configComponent) : default!,
        };
        static FhirParametersGenerator.Tests.NestedConfig BuildNestedConfig(Parameters.ParameterComponent component)
        {
            return new FhirParametersGenerator.Tests.NestedConfig
            {
                Key = (component.Part.FirstOrDefault(p => p.Name == "key")?.Value as FhirString)?.Value ?? string.Empty,
                Enabled = (component.Part.FirstOrDefault(p => p.Name == "enabled")?.Value as FhirBoolean)?.Value ?? default,
            };
        }
    }
}


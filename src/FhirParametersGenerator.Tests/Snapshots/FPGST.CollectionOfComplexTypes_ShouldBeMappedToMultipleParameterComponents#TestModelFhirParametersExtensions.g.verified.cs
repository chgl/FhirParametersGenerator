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
        // System.Collections.Generic.List<FhirParametersGenerator.Tests.RuleConfig> (System.Collections.Generic.List<FhirParametersGenerator.Tests.RuleConfig>) FhirParametersGenerator.Tests.TestModel.Rules
        if (model.Rules != null)
        {
            foreach (var item in model.Rules)
            {
                var rulesComponent = new Parameters.ParameterComponent { Name = "rules" };
                // string (string) FhirParametersGenerator.Tests.RuleConfig.Path
                rulesComponent.Part.Add(new Parameters.ParameterComponent { Name = "path", Value = new FhirString(item.Path) });
                // string (string) FhirParametersGenerator.Tests.RuleConfig.Method
                rulesComponent.Part.Add(new Parameters.ParameterComponent { Name = "method", Value = new FhirString(item.Method) });
                parameters.Parameter.Add(rulesComponent);
            }
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
            Rules = parameters.Get("rules").Select(BuildRuleConfig).ToList(),
        };
        static FhirParametersGenerator.Tests.RuleConfig BuildRuleConfig(Parameters.ParameterComponent component)
        {
            return new FhirParametersGenerator.Tests.RuleConfig
            {
                Path = (component.Part.FirstOrDefault(p => p.Name == "path")?.Value as FhirString)?.Value ?? string.Empty,
                Method = (component.Part.FirstOrDefault(p => p.Name == "method")?.Value as FhirString)?.Value ?? string.Empty,
            };
        }
    }
}


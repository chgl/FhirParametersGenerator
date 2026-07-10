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

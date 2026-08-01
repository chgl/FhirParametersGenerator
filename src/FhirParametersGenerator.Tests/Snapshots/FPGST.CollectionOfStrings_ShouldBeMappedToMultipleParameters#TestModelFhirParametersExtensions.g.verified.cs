//HintName: TestModelFhirParametersExtensions.g.cs
using Hl7.Fhir.Model;
using System;
using System.Linq;
// FhirParametersExtensions generated for type 'TestModel'
namespace FhirParametersGenerator.Tests;

/// <summary>
/// Code-generated extension methods to convert the model class to/from a FHIR Parameters resource.
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
        // System.Collections.Generic.List<string> (System.Collections.Generic.List<string>) FhirParametersGenerator.Tests.TestModel.Tags
        if (model.Tags != null)
        {
            foreach (var item in model.Tags)
            {
                parameters.Add("tags", new FhirString(item));
            }
        }
        return parameters;

    }

    /// <summary>
    /// Convert a FHIR Parameters resource back to an instance of <see cref="TestModel"/>.
    /// </summary>
    /// <param name="parameters">The FHIR Parameters instance.</param>
    /// <returns>A new instance of <see cref="TestModel"/> populated from the given parameters.</returns>
    public static FhirParametersGenerator.Tests.TestModel ToTestModel(this Parameters parameters)
    {
        return new FhirParametersGenerator.Tests.TestModel
        {
            Tags = parameters.Get("tags").Select(p => (p.Value as FhirString)?.Value ?? string.Empty).ToList(),
        };

    }
}

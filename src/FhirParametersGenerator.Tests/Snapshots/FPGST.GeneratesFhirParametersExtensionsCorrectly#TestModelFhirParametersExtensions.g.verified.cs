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
            Age = (int?)(parameters.GetSingle("age")?.Value as FhirDecimal)?.Value ?? default,
            IsSomething = (parameters.GetSingle("isSomething")?.Value as FhirBoolean)?.Value ?? default,
            Timestamp = (parameters.GetSingle("timestamp")?.Value as FhirDateTime)?.ToDateTimeOffset(TimeSpan.Zero) ?? default!,
            Time = (parameters.GetSingle("time")?.Value as FhirDateTime)?.ToDateTimeOffset(TimeSpan.Zero).DateTime ?? default!,
        };
    }
}


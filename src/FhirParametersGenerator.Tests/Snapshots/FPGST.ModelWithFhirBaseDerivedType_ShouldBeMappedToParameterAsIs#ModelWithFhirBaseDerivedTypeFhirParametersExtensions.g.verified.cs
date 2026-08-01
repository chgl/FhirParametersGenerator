//HintName: ModelWithFhirBaseDerivedTypeFhirParametersExtensions.g.cs
using Hl7.Fhir.Model;
using System;
using System.Linq;
// FhirParametersExtensions generated for type 'ModelWithFhirBaseDerivedType'
namespace FhirParametersGenerator.Tests;

/// <summary>
/// Code-generated extension methods to convert the model class to a FHIR Parameters resource.
/// </summary>
public static class ModelWithFhirBaseDerivedTypeFhirParametersExtensions
{
    /// <summary>
    /// Convert the model class to its FHIR Parameters representation.
    /// </summary>
    /// <param name="model">The model class.</param>
    /// <returns>A FHIR Parameters instance.</returns>
    [Obsolete("AsFhirParameters is deprecated, please use ToFhirParameters instead.")]
    public static Parameters AsFhirParameters(this FhirParametersGenerator.Tests.ModelWithFhirBaseDerivedType model)
    {
        return ToFhirParameters(model);
    }

    /// <summary>
    /// Convert the model class to its FHIR Parameters representation.
    /// </summary>
    /// <param name="model">The model class.</param>
    /// <returns>A FHIR Parameters instance.</returns>
    public static Parameters ToFhirParameters(this FhirParametersGenerator.Tests.ModelWithFhirBaseDerivedType model)
    {
        var parameters = new Parameters();
        // Hl7.Fhir.Model.CodeableConcept (Hl7.Fhir.Model.CodeableConcept) FhirParametersGenerator.Tests.ModelWithFhirBaseDerivedType.Code
        parameters.Add("code", model.Code);
        // Hl7.Fhir.Model.Patient (Hl7.Fhir.Model.Patient) FhirParametersGenerator.Tests.ModelWithFhirBaseDerivedType.Patient
        parameters.Add("patient", model.Patient);
        return parameters;

    }
}
partial class ModelWithFhirBaseDerivedType
{
    /// <summary>
    /// Convert a FHIR Parameters resource back to an instance of <see cref="ModelWithFhirBaseDerivedType"/>.
    /// </summary>
    /// <param name="parameters">The FHIR Parameters instance.</param>
    /// <returns>A new instance of <see cref="ModelWithFhirBaseDerivedType"/> populated from the given parameters.</returns>
    public static FhirParametersGenerator.Tests.ModelWithFhirBaseDerivedType FromFhirParameters(Parameters parameters)
    {
        return new FhirParametersGenerator.Tests.ModelWithFhirBaseDerivedType
        {
            Code = parameters.GetSingle("code")?.Value as Hl7.Fhir.Model.CodeableConcept ?? default!,
            Patient = parameters.GetSingle("patient")?.Resource as Hl7.Fhir.Model.Patient ?? default!,
        };
    }
}


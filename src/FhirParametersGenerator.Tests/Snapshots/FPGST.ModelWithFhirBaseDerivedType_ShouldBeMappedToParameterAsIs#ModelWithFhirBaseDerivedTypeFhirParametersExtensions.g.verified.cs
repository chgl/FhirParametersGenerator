//HintName: ModelWithFhirBaseDerivedTypeFhirParametersExtensions.g.cs
using Hl7.Fhir.Model;

// FhirParametersExtensions generated for type 'ModelWithFhirBaseDerivedType'
namespace FhirParametersGenerator.Tests;

public static class ModelWithFhirBaseDerivedTypeFhirParametersExtensions
{
    [Obsolete("AsFhirParameters is deprecated, please use ToFhirParameters instead.")]
    public static Parameters AsFhirParameters(
        this FhirParametersGenerator.Tests.ModelWithFhirBaseDerivedType model
    )
    {
        return ToFhirParameters(model);
    }

    public static Parameters ToFhirParameters(
        this FhirParametersGenerator.Tests.ModelWithFhirBaseDerivedType model
    )
    {
        var parameters = new Parameters();
        // Hl7.Fhir.Model.CodeableConcept (Hl7.Fhir.Model.CodeableConcept) FhirParametersGenerator.Tests.ModelWithFhirBaseDerivedType.Code
        parameters.Add("code", model.Code);
        // Hl7.Fhir.Model.Patient (Hl7.Fhir.Model.Patient) FhirParametersGenerator.Tests.ModelWithFhirBaseDerivedType.Patient
        parameters.Add("patient", model.Patient);
        return parameters;
    }
}

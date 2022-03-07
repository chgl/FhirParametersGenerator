using System;
using System.Diagnostics.CodeAnalysis;

namespace FhirParametersGenerator;

/// <summary>
/// Marker attribute indicating model classes for which a FHIR Parameters
/// resource mapping extension method should be code-generated.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class GenerateFhirParametersAttribute : Attribute
{
}

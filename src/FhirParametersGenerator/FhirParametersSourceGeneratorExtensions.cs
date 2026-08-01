using Microsoft.CodeAnalysis;

internal static class FhirParametersSourceGeneratorExtensions
{
    // via <https://www.meziantou.net/working-with-types-in-a-roslyn-analyzer.htm>
    public static bool InheritsFrom(this ITypeSymbol symbol, string typeDisplayName)
    {
        INamedTypeSymbol? baseType = symbol.BaseType;
        while (baseType != null)
        {
            var baseTypeDisplayName = baseType.ToDisplayString();

            if (baseTypeDisplayName == typeDisplayName)
            {
                return true;
            }

            baseType = baseType.BaseType;
        }

        return false;
    }

    // `InheritsFrom` deliberately only walks strict ancestors. Callers that need an "is-a" check
    // (does this type belong to the given family, including exactly being it) — e.g. deciding
    // whether a property typed exactly `Resource` or `Base` needs the Resource/DataType handling —
    // should use this instead, or they'll incorrectly treat the exact base type as unrelated.
    // Compares with the nullable annotation stripped: a property declared `Resource?` must still
    // match "Hl7.Fhir.Model.Resource", not "Hl7.Fhir.Model.Resource?".
    public static bool IsOrInheritsFrom(this ITypeSymbol symbol, string typeDisplayName) =>
        symbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString()
            == typeDisplayName
        || symbol.InheritsFrom(typeDisplayName);
}

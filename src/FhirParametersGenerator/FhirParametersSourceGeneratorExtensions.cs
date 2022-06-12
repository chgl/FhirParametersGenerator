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
}

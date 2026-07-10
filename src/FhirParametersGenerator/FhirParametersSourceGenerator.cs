using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace FhirParametersGenerator;

/// <inheritdoc />
// This code is based on Andrew Lock's brilliant https://andrewlock.net/creating-a-source-generator-part-1-creating-an-incremental-source-generator/
[Generator]
public class FhirParametersSourceGenerator : IIncrementalGenerator
{
    private const string FhirParametersGeneratorAttributeFullName =
        "FhirParametersGenerator.GenerateFhirParametersAttribute";

    private static readonly Dictionary<string, string> ClrTypeToFhirType;

    static FhirParametersSourceGenerator()
    {
        ClrTypeToFhirType = new Dictionary<string, string>()
        {
            ["Int32"] = "FhirDecimal",
            ["String"] = "FhirString",
            ["Boolean"] = "FhirBoolean",
            ["DateTime"] = "FhirDateTime",
            ["DateTimeOffset"] = "FhirDateTime",
        };
    }

    /// <inheritdoc />
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Do a simple filter for enums
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context
            .SyntaxProvider.CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx)
            )
            .Where(static m => m is not null)!;

        // Combine the selected class with the `Compilation`
        IncrementalValueProvider<(
            Compilation,
            ImmutableArray<ClassDeclarationSyntax>
        )> compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

        // Generate the source using the compilation and enums
        context.RegisterSourceOutput(
            compilationAndClasses,
            static (spc, source) => Execute(source.Item1, source.Item2, spc)
        );
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        if (node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0)
        {
            return true;
        }

        return false;
    }

    static ClassDeclarationSyntax? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

        // loop through all the attributes on the class
        foreach (var attributeListSyntax in classDeclarationSyntax.AttributeLists)
        {
            foreach (var attributeSyntax in attributeListSyntax.Attributes)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attributeSyntax);
                var attributeSymbol = symbolInfo.Symbol;
                // attributes are actually methods (the unabbreviated syntax is [SomeAttribute()] after all)
                if (attributeSymbol is not IMethodSymbol)
                {
                    // weird, we couldn't get the symbol, ignore it
                    continue;
                }

                var attributeContainingTypeSymbol = attributeSymbol.ContainingType;
                var fullName = attributeContainingTypeSymbol.ToDisplayString();

                // is the attribute the [GenerateFhirParametersAttribute] attribute?
                if (fullName == FhirParametersGeneratorAttributeFullName)
                {
                    // return the class
                    return classDeclarationSyntax;
                }
            }
        }

        // none of the attributes are of our type
        return null;
    }

    static void Execute(
        Compilation compilation,
        ImmutableArray<ClassDeclarationSyntax> classes,
        SourceProductionContext context
    )
    {
        if (classes.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return;
        }

        var distinctClasses = classes.Distinct();

        // Convert each ClassDeclarationSyntax to their INamedSymbol
        var classesToGenerate = GetTypesToGenerate(
            compilation,
            distinctClasses,
            context.CancellationToken
        );
        if (classesToGenerate.Count == 0)
        {
            return;
        }

        foreach (var classToGenerate in classesToGenerate)
        {
            var generatedSourceFileName = $"{classToGenerate.Name}FhirParametersExtensions.g.cs";
            var source = GenerateExtensionClass(classToGenerate, context);
            context.AddSource(generatedSourceFileName, SourceText.From(source, Encoding.UTF8));
        }
    }

    static List<INamedTypeSymbol> GetTypesToGenerate(
        Compilation compilation,
        IEnumerable<ClassDeclarationSyntax> classes,
        CancellationToken ct
    )
    {
        // Create a list to hold our output
        var classesToGenerate = new List<INamedTypeSymbol>();

        // Get the semantic representation of our marker attribute
        INamedTypeSymbol? generatorAttribute = compilation.GetTypeByMetadataName(
            FhirParametersGeneratorAttributeFullName
        );

        if (generatorAttribute == null)
        {
            // If this is null, the compilation couldn't find the marker attribute type
            // which suggests there's something very wrong! Bail out..
            return classesToGenerate;
        }

        foreach (var classDeclarationSyntax in classes)
        {
            // stop if we're asked to
            ct.ThrowIfCancellationRequested();

            // Get the semantic representation of the class syntax
            SemanticModel semanticModel = compilation.GetSemanticModel(
                classDeclarationSyntax.SyntaxTree
            );
            if (
                semanticModel.GetDeclaredSymbol(classDeclarationSyntax)
                is not INamedTypeSymbol classSymbol
            )
            {
                // something went wrong, bail out
                continue;
            }

            classesToGenerate.Add(classSymbol);
        }

        return classesToGenerate;
    }

    static string GenerateExtensionClass(
        INamedTypeSymbol classSymbol,
        SourceProductionContext context
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine("using Hl7.Fhir.Model;");

        sb.AppendLine($"// FhirParametersExtensions generated for type '{classSymbol.Name}'");

        var containingNamespace = classSymbol.ContainingNamespace;

        if (!containingNamespace.IsGlobalNamespace)
        {
            // place the generated extensions in the same namespace as the original class
            sb.AppendLine($"namespace {containingNamespace.ToDisplayString()};");
        }

        var methodBody = GenerateMappingMethodBody(classSymbol, context);

        var source =
            $@"
/// <summary>
/// Code-generated extension methods to convert the model class to a FHIR Parameters resources.
/// </summary>
public static class {classSymbol.Name}FhirParametersExtensions
{{
    /// <summary>
    /// Convert the model class to its FHIR Parameters representation.
    /// </summary>
    /// <param name=""model"">The model class.</param>
    /// <returns>A FHIR Parameters instance.</returns>
    [Obsolete(""AsFhirParameters is deprecated, please use ToFhirParameters instead."")]
    public static Parameters AsFhirParameters(this {classSymbol.ToDisplayString()} model)
    {{
        return ToFhirParameters(model);
    }}

    /// <summary>
    /// Convert the model class to its FHIR Parameters representation.
    /// </summary>
    /// <param name=""model"">The model class.</param>
    /// <returns>A FHIR Parameters instance.</returns>
    public static Parameters ToFhirParameters(this {classSymbol.ToDisplayString()} model)
    {{
{methodBody}
    }}
}}";

        sb.AppendLine(source);

        return sb.ToString();
    }

    static string GenerateMappingMethodBody(
        INamedTypeSymbol classSymbol,
        SourceProductionContext context
    )
    {
        var indent = new string(' ', 8);
        var sourceBuilder = new StringBuilder();

        sourceBuilder.Append(indent);
        sourceBuilder.AppendLine("var parameters = new Parameters();");

        var visitedTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            classSymbol.ToDisplayString()
        };

        foreach (var property in GetAllReadableProperties(classSymbol))
        {
            sourceBuilder.Append(indent);
            sourceBuilder.AppendLine(
                $"// {property.Type} ({property.Type.ToDisplayString()}) {property.ToDisplayString()}"
            );
            GenerateTopLevelPropertyCode(property, "model", indent, sourceBuilder, context, visitedTypes);
        }

        sourceBuilder.Append(indent);
        sourceBuilder.AppendLine("return parameters;");

        return sourceBuilder.ToString();
    }

    static void GenerateTopLevelPropertyCode(
        IPropertySymbol property,
        string modelPath,
        string indent,
        StringBuilder sb,
        SourceProductionContext context,
        HashSet<string> visitedTypes
    )
    {
        var camelCasedName = ConvertNameToCamelCase(property.Name);
        var propAccess = $"{modelPath}.{property.Name}";
        var propType = UnwrapNullable(property.Type);

        if (propType.InheritsFrom("Hl7.Fhir.Model.Base"))
        {
            sb.Append(indent);
            sb.AppendLine($@"parameters.Add(""{camelCasedName}"", {propAccess});");
            return;
        }

        var fhirExpr = GetFhirValueExpression(propType, propAccess);
        if (fhirExpr != null)
        {
            sb.Append(indent);
            sb.AppendLine($@"parameters.Add(""{camelCasedName}"", {fhirExpr});");
            return;
        }

        if (TryGetEnumerableElementType(propType, out var elementType))
        {
            var itemExpr = GetFhirValueExpression(elementType, "item");
            if (itemExpr != null)
            {
                sb.Append(indent);
                sb.AppendLine($"if ({propAccess} != null)");
                sb.Append(indent);
                sb.AppendLine("{");
                var inner = indent + "    ";
                sb.Append(inner);
                sb.AppendLine($"foreach (var item in {propAccess})");
                sb.Append(inner);
                sb.AppendLine("{");
                sb.Append(inner + "    ");
                sb.AppendLine($@"parameters.Add(""{camelCasedName}"", {itemExpr});");
                sb.Append(inner);
                sb.AppendLine("}");
                sb.Append(indent);
                sb.AppendLine("}");
            }
            else if (
                IsUserDefinedClass(elementType)
                && elementType is INamedTypeSymbol elementClassType
                && !visitedTypes.Contains(elementClassType.ToDisplayString())
            )
            {
                visitedTypes.Add(elementClassType.ToDisplayString());
                var compVar = $"{camelCasedName}Component";
                sb.Append(indent);
                sb.AppendLine($"if ({propAccess} != null)");
                sb.Append(indent);
                sb.AppendLine("{");
                var inner = indent + "    ";
                sb.Append(inner);
                sb.AppendLine($"foreach (var item in {propAccess})");
                sb.Append(inner);
                sb.AppendLine("{");
                var innerInner = inner + "    ";
                sb.Append(innerInner);
                sb.AppendLine(
                    $@"var {compVar} = new Parameters.ParameterComponent {{ Name = ""{camelCasedName}"" }};"
                );
                GenerateNestedParts(
                    elementClassType,
                    "item",
                    compVar,
                    innerInner,
                    sb,
                    context,
                    visitedTypes
                );
                sb.Append(innerInner);
                sb.AppendLine($"parameters.Parameter.Add({compVar});");
                sb.Append(inner);
                sb.AppendLine("}");
                sb.Append(indent);
                sb.AppendLine("}");
                visitedTypes.Remove(elementClassType.ToDisplayString());
            }
            else
            {
                ReportUnsupportedPropertyTypeDiagnostic(property, context);
                sb.Append(indent);
                sb.AppendLine(
                    $@"parameters.Add(""{camelCasedName}"", new FhirString({propAccess}?.ToString()));"
                );
            }
            return;
        }

        if (IsUserDefinedClass(propType) && propType is INamedTypeSymbol nestedType)
        {
            var fullName = nestedType.ToDisplayString();
            if (!visitedTypes.Contains(fullName))
            {
                visitedTypes.Add(fullName);
                var compVar =
                    $"{char.ToLowerInvariant(property.Name[0])}{property.Name.Substring(1)}Component";
                sb.Append(indent);
                sb.AppendLine($"if ({propAccess} != null)");
                sb.Append(indent);
                sb.AppendLine("{");
                var inner = indent + "    ";
                sb.Append(inner);
                sb.AppendLine(
                    $@"var {compVar} = new Parameters.ParameterComponent {{ Name = ""{camelCasedName}"" }};"
                );
                GenerateNestedParts(nestedType, propAccess, compVar, inner, sb, context, visitedTypes);
                sb.Append(inner);
                sb.AppendLine($"parameters.Parameter.Add({compVar});");
                sb.Append(indent);
                sb.AppendLine("}");
                visitedTypes.Remove(fullName);
            }
            return;
        }

        ReportUnsupportedPropertyTypeDiagnostic(property, context);
        sb.Append(indent);
        sb.AppendLine(
            $@"parameters.Add(""{camelCasedName}"", new FhirString({propAccess}?.ToString()));"
        );
    }

    static void GenerateNestedParts(
        INamedTypeSymbol classSymbol,
        string modelPath,
        string compVar,
        string indent,
        StringBuilder sb,
        SourceProductionContext context,
        HashSet<string> visitedTypes
    )
    {
        foreach (var property in GetAllReadableProperties(classSymbol))
        {
            sb.Append(indent);
            sb.AppendLine(
                $"// {property.Type} ({property.Type.ToDisplayString()}) {property.ToDisplayString()}"
            );

            var camelCasedName = ConvertNameToCamelCase(property.Name);
            var propAccess = $"{modelPath}.{property.Name}";
            var propType = UnwrapNullable(property.Type);

            if (propType.InheritsFrom("Hl7.Fhir.Model.Base"))
            {
                if (propType.InheritsFrom("Hl7.Fhir.Model.Resource"))
                {
                    sb.Append(indent);
                    sb.AppendLine(
                        $@"if ({propAccess} != null) {compVar}.Part.Add(new Parameters.ParameterComponent {{ Name = ""{camelCasedName}"", Resource = {propAccess} }});"
                    );
                }
                else
                {
                    sb.Append(indent);
                    sb.AppendLine(
                        $@"if ({propAccess} != null) {compVar}.Part.Add(new Parameters.ParameterComponent {{ Name = ""{camelCasedName}"", Value = {propAccess} }});"
                    );
                }
                continue;
            }

            var fhirExpr = GetFhirValueExpression(propType, propAccess);
            if (fhirExpr != null)
            {
                sb.Append(indent);
                sb.AppendLine(
                    $@"{compVar}.Part.Add(new Parameters.ParameterComponent {{ Name = ""{camelCasedName}"", Value = {fhirExpr} }});"
                );
                continue;
            }

            if (TryGetEnumerableElementType(propType, out var elementType))
            {
                var itemExpr = GetFhirValueExpression(elementType, "item");
                if (itemExpr != null)
                {
                    sb.Append(indent);
                    sb.AppendLine($"if ({propAccess} != null)");
                    sb.Append(indent);
                    sb.AppendLine("{");
                    var inner = indent + "    ";
                    sb.Append(inner);
                    sb.AppendLine($"foreach (var item in {propAccess})");
                    sb.Append(inner);
                    sb.AppendLine("{");
                    sb.Append(inner + "    ");
                    sb.AppendLine(
                        $@"{compVar}.Part.Add(new Parameters.ParameterComponent {{ Name = ""{camelCasedName}"", Value = {itemExpr} }});"
                    );
                    sb.Append(inner);
                    sb.AppendLine("}");
                    sb.Append(indent);
                    sb.AppendLine("}");
                }
                else if (
                    IsUserDefinedClass(elementType)
                    && elementType is INamedTypeSymbol elementClassType
                    && !visitedTypes.Contains(elementClassType.ToDisplayString())
                )
                {
                    visitedTypes.Add(elementClassType.ToDisplayString());
                    var innerCompVar = $"{camelCasedName}Component";
                    sb.Append(indent);
                    sb.AppendLine($"if ({propAccess} != null)");
                    sb.Append(indent);
                    sb.AppendLine("{");
                    var inner = indent + "    ";
                    sb.Append(inner);
                    sb.AppendLine($"foreach (var item in {propAccess})");
                    sb.Append(inner);
                    sb.AppendLine("{");
                    var innerInner = inner + "    ";
                    sb.Append(innerInner);
                    sb.AppendLine(
                        $@"var {innerCompVar} = new Parameters.ParameterComponent {{ Name = ""{camelCasedName}"" }};"
                    );
                    GenerateNestedParts(
                        elementClassType,
                        "item",
                        innerCompVar,
                        innerInner,
                        sb,
                        context,
                        visitedTypes
                    );
                    sb.Append(innerInner);
                    sb.AppendLine($"{compVar}.Part.Add({innerCompVar});");
                    sb.Append(inner);
                    sb.AppendLine("}");
                    sb.Append(indent);
                    sb.AppendLine("}");
                    visitedTypes.Remove(elementClassType.ToDisplayString());
                }
                else
                {
                    ReportUnsupportedPropertyTypeDiagnostic(property, context);
                    sb.Append(indent);
                    sb.AppendLine(
                        $@"if ({propAccess} != null) {compVar}.Part.Add(new Parameters.ParameterComponent {{ Name = ""{camelCasedName}"", Value = new FhirString({propAccess}.ToString()) }});"
                    );
                }
                continue;
            }

            if (IsUserDefinedClass(propType) && propType is INamedTypeSymbol nestedType)
            {
                var fullName = nestedType.ToDisplayString();
                if (!visitedTypes.Contains(fullName))
                {
                    visitedTypes.Add(fullName);
                    var innerCompVar =
                        $"{char.ToLowerInvariant(property.Name[0])}{property.Name.Substring(1)}Component";
                    sb.Append(indent);
                    sb.AppendLine($"if ({propAccess} != null)");
                    sb.Append(indent);
                    sb.AppendLine("{");
                    var inner = indent + "    ";
                    sb.Append(inner);
                    sb.AppendLine(
                        $@"var {innerCompVar} = new Parameters.ParameterComponent {{ Name = ""{camelCasedName}"" }};"
                    );
                    GenerateNestedParts(
                        nestedType,
                        propAccess,
                        innerCompVar,
                        inner,
                        sb,
                        context,
                        visitedTypes
                    );
                    sb.Append(inner);
                    sb.AppendLine($"{compVar}.Part.Add({innerCompVar});");
                    sb.Append(indent);
                    sb.AppendLine("}");
                    visitedTypes.Remove(fullName);
                }
                continue;
            }

            ReportUnsupportedPropertyTypeDiagnostic(property, context);
            sb.Append(indent);
            sb.AppendLine(
                $@"if ({propAccess} != null) {compVar}.Part.Add(new Parameters.ParameterComponent {{ Name = ""{camelCasedName}"", Value = new FhirString({propAccess}.ToString()) }});"
            );
        }
    }

    // Returns a FHIR constructor expression for the given CLR type and value expression,
    // or null if the type requires a different mapping strategy.
    static string? GetFhirValueExpression(ITypeSymbol type, string valueExpr)
    {
        if (ClrTypeToFhirType.TryGetValue(type.Name, out var fhirTypeName))
            return $"new {fhirTypeName}({valueExpr})";

        if (type.TypeKind == TypeKind.Enum)
            return $"new FhirString({valueExpr}.ToString())";

        return null;
    }

    // Unwraps Nullable<T> to T so we can inspect the underlying type.
    static ITypeSymbol UnwrapNullable(ITypeSymbol type)
    {
        if (
            type is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
        )
            return namedType.TypeArguments[0];
        return type;
    }

    // Returns true when the type is an IEnumerable<T> (but not string), setting elementType to T.
    static bool TryGetEnumerableElementType(ITypeSymbol type, out ITypeSymbol elementType)
    {
        elementType = null!;

        // string implements IEnumerable<char> but we don't want to treat it as a char collection
        if (type.SpecialType == SpecialType.System_String)
            return false;

        if (type is IArrayTypeSymbol arrayType)
        {
            elementType = arrayType.ElementType;
            return true;
        }

        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            // IEnumerable<T> itself
            if (
                namedType.OriginalDefinition.SpecialType
                == SpecialType.System_Collections_Generic_IEnumerable_T
            )
            {
                elementType = namedType.TypeArguments[0];
                return true;
            }

            // Types that implement IEnumerable<T> (List<T>, HashSet<T>, etc.)
            var enumerableIface = namedType.AllInterfaces.FirstOrDefault(
                i =>
                    i.OriginalDefinition.SpecialType
                    == SpecialType.System_Collections_Generic_IEnumerable_T
            );
            if (enumerableIface != null)
            {
                elementType = enumerableIface.TypeArguments[0];
                return true;
            }
        }

        return false;
    }

    // Returns true for non-generic, non-special classes that should be recursively mapped as
    // nested ParameterComponents. Excludes string, object, and generic collection types.
    static bool IsUserDefinedClass(ITypeSymbol type) =>
        type.TypeKind == TypeKind.Class
        && type.SpecialType == SpecialType.None
        && type is INamedTypeSymbol { IsGenericType: false };

    // Returns all readable, named properties declared on the class and its base types,
    // stopping before System.Object.
    static IEnumerable<IPropertySymbol> GetAllReadableProperties(INamedTypeSymbol classSymbol)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        INamedTypeSymbol? current = classSymbol;
        while (current != null && current.SpecialType != SpecialType.System_Object)
        {
            foreach (var member in current.GetMembers())
            {
                if (
                    member
                    is IPropertySymbol
                    {
                        IsWriteOnly: false,
                        CanBeReferencedByName: true
                    } prop
                )
                {
                    // derived class properties shadow base class properties of the same name
                    if (seen.Add(prop.Name))
                        yield return prop;
                }
            }
            current = current.BaseType;
        }
    }

    static void ReportUnsupportedPropertyTypeDiagnostic(
        IPropertySymbol property,
        SourceProductionContext context
    )
    {
        var descriptor = new DiagnosticDescriptor(
            id: "FHIRPARAMS1",
            title: "Unsupported property type",
            messageFormat: $"Unable to map property {property.ToDisplayString()} of type {property.Type.ToDisplayString()} to a FHIR representation. "
                + $"Defaulting to FhirString with a value of {property.ToDisplayString()}.ToString().",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        var location = property.Locations.FirstOrDefault();
        var diagnostic = Diagnostic.Create(descriptor, location);

        context.ReportDiagnostic(diagnostic);
    }

    // Code from https://github.com/dotnet/runtime/blob/v6.0.2/src/libraries/System.Text.Json/Common/JsonCamelCaseNamingPolicy.cs
    // licensed under the MIT License
    static string ConvertNameToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || !char.IsUpper(name[0]))
        {
            return name;
        }

        char[] chars = name.ToCharArray();
        FixCasing(chars);
        return new string(chars);
    }

    static void FixCasing(Span<char> chars)
    {
        for (int i = 0; i < chars.Length; i++)
        {
            if (i == 1 && !char.IsUpper(chars[i]))
            {
                break;
            }

            bool hasNext = (i + 1 < chars.Length);

            // Stop when next char is already lowercase.
            if (i > 0 && hasNext && !char.IsUpper(chars[i + 1]))
            {
                // If the next char is a space, lowercase current char before exiting.
                if (chars[i + 1] == ' ')
                {
                    chars[i] = char.ToLowerInvariant(chars[i]);
                }

                break;
            }

            chars[i] = char.ToLowerInvariant(chars[i]);
        }
    }
}

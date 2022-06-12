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
    private const string FhirParametersGeneratorAttributeFullName = "FhirParametersGenerator.GenerateFhirParametersAttribute";

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
        IncrementalValuesProvider<ClassDeclarationSyntax> classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null)!;

        // Combine the selected class with the `Compilation`
        IncrementalValueProvider<(Compilation, ImmutableArray<ClassDeclarationSyntax>)> compilationAndClasses
            = context.CompilationProvider.Combine(classDeclarations.Collect());

        // Generate the source using the compilation and enums
        context.RegisterSourceOutput(compilationAndClasses,
            static (spc, source) => Execute(source.Item1, source.Item2, spc));
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        if (node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0)
        {
            return true;
        }

        // if (node is StructDeclarationSyntax sds && sds.AttributeLists.Count > 0)
        // {
        //     return true;
        // }

        // if (node is RecordDeclarationSyntax rds && rds.AttributeLists.Count > 0)
        // {
        //     return true;
        // }

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

    static void Execute(Compilation compilation, ImmutableArray<ClassDeclarationSyntax> classes, SourceProductionContext context)
    {
        if (classes.IsDefaultOrEmpty)
        {
            // nothing to do yet
            return;
        }

        var distinctClasses = classes.Distinct();

        // Convert each ClassDeclarationSyntax to their INamedSymbol
        var classesToGenerate = GetTypesToGenerate(compilation, distinctClasses, context.CancellationToken);
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

    static List<INamedTypeSymbol> GetTypesToGenerate(Compilation compilation, IEnumerable<ClassDeclarationSyntax> classes, CancellationToken ct)
    {
        // Create a list to hold our output
        var classesToGenerate = new List<INamedTypeSymbol>();

        // Get the semantic representation of our marker attribute
        INamedTypeSymbol? generatorAttribute = compilation.GetTypeByMetadataName(FhirParametersGeneratorAttributeFullName);

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
            SemanticModel semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classDeclarationSyntax) is not INamedTypeSymbol classSymbol)
            {
                // something went wrong, bail out
                continue;
            }

            // Get the full type name of the class
            // or OuterClass<T>.InnerClass if it was nested in a generic type (for example)
            string className = classSymbol.ToString();

            classesToGenerate.Add(classSymbol);
        }

        return classesToGenerate;
    }

    static string GenerateExtensionClass(INamedTypeSymbol classSymbol, SourceProductionContext context)
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

        var source = $@"
public static class {classSymbol.Name}FhirParametersExtensions
{{
    [Obsolete(""AsFhirParameters is deprecated, please use ToFhirParameters instead."")]
    public static Parameters AsFhirParameters(this {classSymbol.ToDisplayString()} model)
    {{
        return ToFhirParameters(model);
    }}

    public static Parameters ToFhirParameters(this {classSymbol.ToDisplayString()} model)
    {{
{methodBody}
    }}
}}";

        sb.AppendLine(source);

        return sb.ToString();
    }

    static string GenerateMappingMethodBody(INamedTypeSymbol classSymbol, SourceProductionContext context)
    {
        var indent = new string(' ', 8);
        var sourceBuilder = new StringBuilder();

        sourceBuilder.Append(indent);
        sourceBuilder.AppendLine("var parameters = new Parameters();");

        // TODO: for each property/field (maybe unless annotated as ignored) call Parameters::Add(field.inCamelCase, new FhirString(field.Value) or if type==int, new FhirInt...)
        // Get all the members in the class
        var classMembers = classSymbol.GetMembers();

        // Get all the fields from the class
        foreach (var member in classMembers)
        {
            // only check properties that are not write-only and can be referenced by their name - because the
            // latter is exactly what we plan on doing
            // we could also check for member-annotations here that exclude a property
            if (member is IPropertySymbol { IsWriteOnly: false, CanBeReferencedByName: true } property)
            {
                sourceBuilder.Append(indent);
                sourceBuilder.AppendLine($"// {property.Type} ({property.Type.ToDisplayString()}) {property.ToDisplayString()}");

                var camelCasedPropertyName = ConvertNameToCamelCase(property.Name);

                var propertyType = property.Type;

                // could be improved by using another ITypeSymbol as the searched for type which is statically
                // fetched from the compilation context via: compilation.GetTypeByMetadataName("Hl7.Fhir.Model.Base");
                if (propertyType.InheritsFrom("Hl7.Fhir.Model.Base"))
                {
                    sourceBuilder.Append(indent);
                    sourceBuilder.AppendLine($@"parameters.Add(""{camelCasedPropertyName}"", model.{property.Name});");
                }
                else
                {
                    var isKnownType = ClrTypeToFhirType.TryGetValue(property.Type.Name, out var fhirTypeName);
                    if (isKnownType)
                    {
                        sourceBuilder.Append(indent);
                        sourceBuilder.AppendLine($@"parameters.Add(""{camelCasedPropertyName}"", new {fhirTypeName}(model.{property.Name}));");
                    }
                    else
                    {
                        ReportUnsupportedPropertyTypeDiagnostic(property, context);

                        sourceBuilder.Append(indent);
                        sourceBuilder.AppendLine($@"parameters.Add(""{camelCasedPropertyName}"", new FhirString(model.{property.Name}.ToString()));");
                    }
                }
            }
        }

        sourceBuilder.Append(indent);
        sourceBuilder.AppendLine("return parameters;");

        return sourceBuilder.ToString();
    }

    static void ReportUnsupportedPropertyTypeDiagnostic(IPropertySymbol property, SourceProductionContext context)
    {
        var descriptor = new DiagnosticDescriptor(
            id: "FHIRPARAMS1",
            title: "Unsupported property type",
            messageFormat: $"Unable to map property {property.ToDisplayString()} of type {property.Type.ToDisplayString()} to a FHIR representation. " +
                $"Defaulting to FhirString with a value of {property.ToDisplayString()}.ToString().",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

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

        return string.Create(name.Length, name, (chars, name) =>
        {
            name.AsSpan().CopyTo(chars);

            FixCasing(chars);
        });
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

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
            ["Int32"] = "Integer",
            ["Int64"] = "Integer64",
            ["Decimal"] = "FhirDecimal",
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
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Linq;");

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
/// Code-generated extension methods to convert the model class to a FHIR Parameters resource.
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

        // The reverse direction (FromFhirParameters) is generated as a real static method on the
        // class itself rather than an extension method, which requires the class - and every
        // enclosing type, since a generator can only add members via a partial declaration - to
        // be declared `partial`. Skip it (with a diagnostic) rather than emit a class declaration
        // that would conflict with the user's non-partial one.
        var missingPartialType = FindFirstNonPartialTypeInChain(classSymbol);
        if (missingPartialType is null)
        {
            sb.AppendLine(GenerateFromFhirParametersPartialClass(classSymbol, context));
        }
        else
        {
            ReportTypeMustBePartialDiagnostic(classSymbol, missingPartialType, context);
        }

        return sb.ToString();
    }

    // Walks a type and its containing types (outermost first) looking for the first one that
    // isn't declared `partial`. Returns null if the whole chain already supports adding a new
    // partial declaration.
    static INamedTypeSymbol? FindFirstNonPartialTypeInChain(INamedTypeSymbol classSymbol)
    {
        for (var current = classSymbol; current != null; current = current.ContainingType)
        {
            if (!IsDeclaredPartial(current))
            {
                return current;
            }
        }

        return null;
    }

    static bool IsDeclaredPartial(INamedTypeSymbol type) =>
        type.DeclaringSyntaxReferences.Any(syntaxRef =>
            syntaxRef.GetSyntax() is TypeDeclarationSyntax { Modifiers: var modifiers }
            && modifiers.Any(SyntaxKind.PartialKeyword)
        );

    // Re-opens `classSymbol` (and every containing type) as `partial` and adds the
    // `FromFhirParameters` static factory method to the innermost one.
    static string GenerateFromFhirParametersPartialClass(
        INamedTypeSymbol classSymbol,
        SourceProductionContext context
    )
    {
        List<INamedTypeSymbol> chain = [];
        for (var current = classSymbol; current != null; current = current.ContainingType)
        {
            chain.Insert(0, current);
        }

        var sb = new StringBuilder();
        var indent = "";

        foreach (var type in chain)
        {
            var typeParameters =
                type.TypeParameters.Length > 0
                    ? $"<{string.Join(", ", type.TypeParameters.Select(tp => tp.Name))}>"
                    : "";

            sb.Append(indent);
            sb.AppendLine($"partial class {type.Name}{typeParameters}");
            sb.Append(indent);
            sb.AppendLine("{");
            indent += "    ";
        }

        var parseMethodBody = GenerateFromFhirParametersMethodBody(
            classSymbol,
            indent + "    ",
            context
        );

        sb.Append(indent);
        sb.AppendLine("/// <summary>");
        sb.Append(indent);
        sb.AppendLine(
            $@"/// Convert a FHIR Parameters resource back to an instance of <see cref=""{classSymbol.Name}""/>."
        );
        sb.Append(indent);
        sb.AppendLine("/// </summary>");
        sb.Append(indent);
        sb.AppendLine(@"/// <param name=""parameters"">The FHIR Parameters instance.</param>");
        sb.Append(indent);
        sb.AppendLine(
            $@"/// <returns>A new instance of <see cref=""{classSymbol.Name}""/> populated from the given parameters.</returns>"
        );
        sb.Append(indent);
        sb.AppendLine(
            $"public static {DisplayTypeName(classSymbol)} FromFhirParameters(Parameters parameters)"
        );
        sb.Append(indent);
        sb.AppendLine("{");
        sb.Append(parseMethodBody);
        sb.Append(indent);
        sb.AppendLine("}");

        for (var i = chain.Count - 1; i >= 0; i--)
        {
            indent = indent.Substring(0, indent.Length - 4);
            sb.Append(indent);
            sb.AppendLine("}");
        }

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
            classSymbol.ToDisplayString(),
        };

        foreach (var property in GetAllReadableProperties(classSymbol))
        {
            sourceBuilder.Append(indent);
            sourceBuilder.AppendLine(
                $"// {property.Type} ({property.Type.ToDisplayString()}) {property.ToDisplayString()}"
            );
            GenerateTopLevelPropertyCode(
                property,
                "model",
                indent,
                sourceBuilder,
                context,
                visitedTypes
            );
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

        if (propType.IsOrInheritsFrom("Hl7.Fhir.Model.Base"))
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
                GenerateNestedParts(
                    nestedType,
                    propAccess,
                    compVar,
                    inner,
                    sb,
                    context,
                    visitedTypes
                );
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

            if (propType.IsOrInheritsFrom("Hl7.Fhir.Model.Base"))
            {
                if (propType.IsOrInheritsFrom("Hl7.Fhir.Model.Resource"))
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

    // Generates the body of the `To{ClassName}(this Parameters parameters)` method: a single
    // object-initializer expression (so `init`-only properties work) plus, appended afterwards,
    // any `Build{Type}` local functions needed to reconstruct nested user-defined types.
    static string GenerateFromFhirParametersMethodBody(
        INamedTypeSymbol classSymbol,
        string indent,
        SourceProductionContext context
    )
    {
        var sb = new StringBuilder();
        var pendingBuilders = new Queue<INamedTypeSymbol>();
        var queuedOrEmitted = new HashSet<string>(StringComparer.Ordinal);
        var localFunctions = new StringBuilder();

        sb.Append(indent);
        sb.AppendLine($"return new {DisplayTypeName(classSymbol)}");
        sb.Append(indent);
        sb.AppendLine("{");

        GenerateObjectInitializerMembers(
            classSymbol,
            name => $@"parameters.GetSingle(""{name}"")",
            name => $@"parameters.Get(""{name}"")",
            indent + "    ",
            sb,
            context,
            pendingBuilders,
            queuedOrEmitted
        );

        sb.Append(indent);
        sb.AppendLine("};");

        // Emit builder functions breadth-first: each one is fully self-contained (open brace to
        // close brace) before the next one starts, so their text is never interleaved. A type
        // referenced from multiple places (or recursively) is only ever enqueued/emitted once.
        while (pendingBuilders.Count > 0)
        {
            var nestedType = pendingBuilders.Dequeue();
            EmitBuilderFunction(
                nestedType,
                indent,
                localFunctions,
                context,
                pendingBuilders,
                queuedOrEmitted
            );
        }

        if (localFunctions.Length > 0)
        {
            sb.Append(localFunctions);
        }

        return sb.ToString();
    }

    // Emits `Prop = <expr>,` for every writable property, used both for the top-level method
    // (reading from `parameters`) and for `Build{Type}` local functions (reading from a
    // `Parameters.ParameterComponent`'s `.Part`), depending on the given lookup expressions.
    static void GenerateObjectInitializerMembers(
        INamedTypeSymbol classSymbol,
        Func<string, string> singleComponentExpr,
        Func<string, string> multiComponentExpr,
        string indent,
        StringBuilder sb,
        SourceProductionContext context,
        Queue<INamedTypeSymbol> pendingBuilders,
        HashSet<string> queuedOrEmitted
    )
    {
        foreach (var property in GetAllReadableProperties(classSymbol))
        {
            if (property.SetMethod is null)
            {
                ReportPropertyHasNoSetterDiagnostic(property, context);
                continue;
            }

            var expr = BuildValueExpression(
                property,
                singleComponentExpr,
                multiComponentExpr,
                context,
                pendingBuilders,
                queuedOrEmitted
            );

            sb.Append(indent);
            sb.AppendLine($"{property.Name} = {expr},");
        }
    }

    // Appends a `static {Type} Build{Type}(Parameters.ParameterComponent component) { ... }`
    // local function that reconstructs a nested user-defined type from a parameter component's
    // `.Part` list. Mirrors `GenerateNestedParts` for the forward direction.
    static void EmitBuilderFunction(
        INamedTypeSymbol nestedType,
        string indent,
        StringBuilder localFunctions,
        SourceProductionContext context,
        Queue<INamedTypeSymbol> pendingBuilders,
        HashSet<string> queuedOrEmitted
    )
    {
        var builderName = GetBuilderName(nestedType);
        var innerIndent = indent + "    ";
        var memberIndent = innerIndent + "    ";

        localFunctions.Append(indent);
        localFunctions.AppendLine(
            $"static {DisplayTypeName(nestedType)} {builderName}(Parameters.ParameterComponent component)"
        );
        localFunctions.Append(indent);
        localFunctions.AppendLine("{");
        localFunctions.Append(innerIndent);
        localFunctions.AppendLine($"return new {DisplayTypeName(nestedType)}");
        localFunctions.Append(innerIndent);
        localFunctions.AppendLine("{");

        GenerateObjectInitializerMembers(
            nestedType,
            name => $@"component.Part.FirstOrDefault(p => p.Name == ""{name}"")",
            name => $@"component.Part.Where(p => p.Name == ""{name}"")",
            memberIndent,
            localFunctions,
            context,
            pendingBuilders,
            queuedOrEmitted
        );

        localFunctions.Append(innerIndent);
        localFunctions.AppendLine("};");
        localFunctions.Append(indent);
        localFunctions.AppendLine("}");
    }

    static string GetBuilderName(INamedTypeSymbol type) => $"Build{type.Name}";

    // `ITypeSymbol.ToDisplayString()` includes a trailing `?` for nullable-annotated reference
    // types (e.g. a property declared `NestedData?`). That's fine in most contexts, but illegal
    // as the type in a `new Type { ... }` object-creation expression (CS8628) and unnecessary
    // noise in casts/generic type arguments, so strip it wherever we need a plain type name.
    static string DisplayTypeName(ITypeSymbol type) =>
        type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString();

    // Ensures a `Build{Type}` local function will be emitted for the given type (queuing it the
    // first time it's referenced from anywhere in the property graph, including recursively from
    // itself) and returns the name to call. Because this only ever queues/emits a type once,
    // self- and mutually-referential types are handled naturally: the generated function simply
    // calls itself, bottoming out at runtime once `.Part` is empty.
    static string EnsureBuilderQueued(
        INamedTypeSymbol type,
        Queue<INamedTypeSymbol> pendingBuilders,
        HashSet<string> queuedOrEmitted
    )
    {
        if (queuedOrEmitted.Add(type.ToDisplayString()))
        {
            pendingBuilders.Enqueue(type);
        }

        return GetBuilderName(type);
    }

    // Builds the C# expression that reads a single property's value back out of a FHIR
    // Parameters/ParameterComponent, for use as the right-hand side of an object-initializer
    // member. `singleComponentExpr`/`multiComponentExpr` abstract over whether we're reading from
    // the top-level `Parameters` or from a nested component's `.Part` list.
    static string BuildValueExpression(
        IPropertySymbol property,
        Func<string, string> singleComponentExpr,
        Func<string, string> multiComponentExpr,
        SourceProductionContext context,
        Queue<INamedTypeSymbol> pendingBuilders,
        HashSet<string> queuedOrEmitted
    )
    {
        var camelCasedName = ConvertNameToCamelCase(property.Name);
        var propType = UnwrapNullable(property.Type);
        var isNullableTarget = IsNullableTarget(property);
        var single = singleComponentExpr(camelCasedName);

        if (propType.IsOrInheritsFrom("Hl7.Fhir.Model.Base"))
        {
            var accessor = propType.IsOrInheritsFrom("Hl7.Fhir.Model.Resource")
                ? "Resource"
                : "Value";
            var expr = $@"{single}?.{accessor} as {DisplayTypeName(propType)}";
            return isNullableTarget
                ? expr
                : $"{expr} ?? {ElseDefaultExpression(propType, isNullableTarget: false)}";
        }

        var scalarExpr = GetPrimitiveConversionExpression(
            propType,
            $"{single}?.Value",
            ElseDefaultExpression(propType, isNullableTarget),
            isNullableTarget,
            camelCasedName
        );
        if (scalarExpr != null)
        {
            return scalarExpr;
        }

        if (TryGetEnumerableElementType(propType, out var elementType))
        {
            return BuildCollectionExpression(
                elementType,
                propType,
                multiComponentExpr,
                camelCasedName,
                property,
                context,
                pendingBuilders,
                queuedOrEmitted
            );
        }

        if (IsUserDefinedClass(propType) && propType is INamedTypeSymbol nestedType)
        {
            var builderName = EnsureBuilderQueued(nestedType, pendingBuilders, queuedOrEmitted);
            var elseExpr = ElseDefaultExpression(propType, isNullableTarget);
            return $@"{single} is {{ }} {camelCasedName}Component ? {builderName}({camelCasedName}Component) : {elseExpr}";
        }

        ReportCannotMapPropertyDiagnostic(property, context);
        return ElseDefaultExpression(propType, isNullableTarget);
    }

    // Builds the `parameters.Get("x").Select(p => ...).ToXxx()` expression for a collection
    // property: primitive elements are converted inline, user-defined element types are routed
    // through a `Build{Type}` local function (queued via `EnsureBuilderQueued`).
    static string BuildCollectionExpression(
        ITypeSymbol elementType,
        ITypeSymbol collectionType,
        Func<string, string> multiComponentExpr,
        string camelCasedName,
        IPropertySymbol property,
        SourceProductionContext context,
        Queue<INamedTypeSymbol> pendingBuilders,
        HashSet<string> queuedOrEmitted
    )
    {
        var multi = multiComponentExpr(camelCasedName);
        var materializer = GetCollectionMaterializer(collectionType);

        var scalarExpr = GetPrimitiveConversionExpression(
            elementType,
            "p.Value",
            ElseDefaultExpression(elementType, isNullableTarget: false),
            isNullableTarget: false,
            uniqueSuffix: "item"
        );
        if (scalarExpr != null)
        {
            return $"{multi}.Select(p => {scalarExpr}){materializer}";
        }

        if (IsUserDefinedClass(elementType) && elementType is INamedTypeSymbol elementClassType)
        {
            var builderName = EnsureBuilderQueued(
                elementClassType,
                pendingBuilders,
                queuedOrEmitted
            );
            return $"{multi}.Select({builderName}){materializer}";
        }

        ReportCannotMapPropertyDiagnostic(property, context);
        return $"Enumerable.Empty<{DisplayTypeName(elementType)}>(){materializer}";
    }

    // Picks the materialization call that matches the property's declared collection type.
    static string GetCollectionMaterializer(ITypeSymbol collectionType)
    {
        if (collectionType is IArrayTypeSymbol)
        {
            return ".ToArray()";
        }

        if (
            collectionType is INamedTypeSymbol { IsGenericType: true } namedType
            && namedType.OriginalDefinition.Name is "HashSet" or "ISet"
        )
        {
            return ".ToHashSet()";
        }

        return ".ToList()";
    }

    // Returns a C# expression converting a `DataType?`-typed expression back to the given CLR
    // type (the reverse of `GetFhirValueExpression`), or null if the type requires a different
    // mapping strategy (collection/nested/unsupported).
    static string? GetPrimitiveConversionExpression(
        ITypeSymbol propType,
        string dataTypeExpr,
        string elseExpr,
        bool isNullableTarget,
        string uniqueSuffix
    )
    {
        if (ClrTypeToFhirType.TryGetValue(propType.Name, out var fhirTypeName))
        {
            string expr;
            switch (fhirTypeName)
            {
                case "FhirString":
                    expr = $"({dataTypeExpr} as FhirString)?.Value";
                    break;
                case "Integer":
                    expr = $"({dataTypeExpr} as Integer)?.Value";
                    break;
                case "Integer64":
                    expr = $"({dataTypeExpr} as Integer64)?.Value";
                    break;
                case "FhirDecimal":
                    expr = $"({dataTypeExpr} as FhirDecimal)?.Value";
                    break;
                case "FhirBoolean":
                    expr = $"({dataTypeExpr} as FhirBoolean)?.Value";
                    break;
                case "FhirDateTime":
                    expr =
                        propType.Name == "DateTimeOffset"
                            ? $"({dataTypeExpr} as FhirDateTime)?.ToDateTimeOffset(TimeSpan.Zero)"
                            : $"({dataTypeExpr} as FhirDateTime)?.ToDateTimeOffset(TimeSpan.Zero).DateTime";
                    break;
                default:
                    return null;
            }

            return isNullableTarget ? expr : $"{expr} ?? {elseExpr}";
        }

        if (propType.TypeKind == TypeKind.Enum)
        {
            var parsedVar = $"{uniqueSuffix}Parsed";
            var enumType = DisplayTypeName(propType);
            var successExpr = isNullableTarget ? $"({enumType}?){parsedVar}" : parsedVar;
            return $@"Enum.TryParse<{enumType}>(({dataTypeExpr} as FhirString)?.Value, out var {parsedVar}) ? {successExpr} : {elseExpr}";
        }

        return null;
    }

    // Whether the original property type allows null: `Nullable<T>` for value types, or a
    // `?`-annotated reference type.
    static bool IsNullableTarget(IPropertySymbol property)
    {
        var type = property.Type;
        if (
            type is INamedTypeSymbol namedType
            && namedType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
        )
        {
            return true;
        }

        return type.NullableAnnotation == NullableAnnotation.Annotated;
    }

    // The fallback expression to use when a value can't be read back from the Parameters
    // resource (missing, or of an unexpected type).
    static string ElseDefaultExpression(ITypeSymbol propType, bool isNullableTarget)
    {
        if (isNullableTarget)
        {
            return "null";
        }

        if (propType.SpecialType == SpecialType.System_String)
        {
            return "string.Empty";
        }

        return propType.IsValueType ? "default" : "default!";
    }

    static void ReportTypeMustBePartialDiagnostic(
        INamedTypeSymbol classSymbol,
        INamedTypeSymbol missingPartialType,
        SourceProductionContext context
    )
    {
        var message = SymbolEqualityComparer.Default.Equals(classSymbol, missingPartialType)
            ? $"Type {missingPartialType.ToDisplayString()} must be declared 'partial' to generate its FromFhirParameters(Parameters) method."
            : $"Type {missingPartialType.ToDisplayString()} must be declared 'partial' so that its nested type {classSymbol.ToDisplayString()} can generate a FromFhirParameters(Parameters) method.";

        var descriptor = new DiagnosticDescriptor(
            id: "FHIRPARAMS4",
            title: "Type must be declared 'partial'",
            messageFormat: message,
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        var location = missingPartialType.Locations.FirstOrDefault();
        var diagnostic = Diagnostic.Create(descriptor, location);

        context.ReportDiagnostic(diagnostic);
    }

    static void ReportPropertyHasNoSetterDiagnostic(
        IPropertySymbol property,
        SourceProductionContext context
    )
    {
        var descriptor = new DiagnosticDescriptor(
            id: "FHIRPARAMS2",
            title: "Property has no accessible setter",
            messageFormat: $"Property {property.ToDisplayString()} has no accessible setter and will not be populated when parsing a FHIR Parameters resource back into this type.",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        var location = property.Locations.FirstOrDefault();
        var diagnostic = Diagnostic.Create(descriptor, location);

        context.ReportDiagnostic(diagnostic);
    }

    static void ReportCannotMapPropertyDiagnostic(
        IPropertySymbol property,
        SourceProductionContext context
    )
    {
        var descriptor = new DiagnosticDescriptor(
            id: "FHIRPARAMS3",
            title: "Unsupported property type when parsing from FHIR Parameters",
            messageFormat: $"Unable to map property {property.ToDisplayString()} of type {property.Type.ToDisplayString()} back from its FHIR Parameters representation. It will be left at its default value.",
            category: "Design",
            defaultSeverity: DiagnosticSeverity.Warning,
            isEnabledByDefault: true
        );

        var location = property.Locations.FirstOrDefault();
        var diagnostic = Diagnostic.Create(descriptor, location);

        context.ReportDiagnostic(diagnostic);
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
            var enumerableIface = namedType.AllInterfaces.FirstOrDefault(i =>
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
                    member is IPropertySymbol
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

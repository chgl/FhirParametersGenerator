using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace FhirParametersGenerator.Tests.Helpers;

public static class TestHelper
{
    [ModuleInitializer]
    public static void Initialize()
    {
        Verifier.DerivePathInfo(
            (_, projectDirectory, type, method) =>
                new(
                    directory: Path.Combine(projectDirectory, "Snapshots"),
                    typeName: TypeNameToAbbreviation(type),
                    methodName: method.Name
                )
        );
    }

    public static Task Verify(string source)
    {
        // Parse the provided string into a C# syntax tree
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain
            .CurrentDomain.GetAssemblies()
            .Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
            .Select(_ => MetadataReference.CreateFromFile(_.Location))
            .Concat(
                new[]
                {
                    MetadataReference.CreateFromFile(
                        typeof(FhirParametersSourceGenerator).Assembly.Location
                    ),
                    MetadataReference.CreateFromFile(
                        typeof(GenerateFhirParametersAttribute).Assembly.Location
                    ),
                    MetadataReference.CreateFromFile(typeof(Hl7.Fhir.Model.Base).Assembly.Location),
                    MetadataReference.CreateFromFile(
                        typeof(Hl7.Fhir.Model.Patient).Assembly.Location
                    ),
                }
            );

        // Create a Roslyn compilation for the syntax tree.
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "SnapshotTests",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new(OutputKind.DynamicallyLinkedLibrary)
        );

        // Create an instance of our FhirParametersSourceGenerator incremental source generator
        var generator = new FhirParametersSourceGenerator();

        // The GeneratorDriver is used to run our generator against a compilation
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        // Run the source generator!
        driver = driver.RunGenerators(compilation);

        // Use verify to snapshot test the source generator output!
        return Verifier.Verify(driver);
    }

    private static string TypeNameToAbbreviation(Type type)
    {
        return string.Concat(type.Name.Where(c => char.IsUpper(c)));
    }
}

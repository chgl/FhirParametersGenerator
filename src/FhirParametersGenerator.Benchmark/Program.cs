using FhirParametersGenerator;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

var summary = BenchmarkRunner.Run(typeof(Benchmarks).Assembly);

public class Benchmarks
{
    private GeneratorDriver Driver { get; init; }
    private CSharpCompilation Compilation { get; init; }

    private readonly string source = @"
using FhirParametersGenerator;

namespace FhirParametersGenerator.Tests;

[GenerateFhirParameters]
public class TestModel
{
    public string Name1 { get; init; } = ""1"";
    public string Name2 { get; init; } = ""2"";
    public string Name3 { get; init; } = ""3"";
    public string Name4 { get; init; } = ""4"";
    public string Name5 { get; init; } = ""5"";
    public string Name6 { get; init; } = ""6"";
    public string Name7 { get; init; } = ""7"";
    public string Name8 { get; init; } = ""8"";
    public string Name9 { get; init; } = ""9"";
    public string Name10 { get; init; } = ""10"";
}";

    public Benchmarks()
    {
        // Parse the provided string into a C# syntax tree
        SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);

        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
            .Select(_ => MetadataReference.CreateFromFile(_.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(FhirParametersSourceGenerator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(GenerateFhirParametersAttribute).Assembly.Location)
            });

        // Create a Roslyn compilation for the syntax tree.
        Compilation = CSharpCompilation.Create(
            assemblyName: "Benchmark",
            syntaxTrees: new[] { syntaxTree },
            references: references,
            options: new(OutputKind.DynamicallyLinkedLibrary));

        // Create an instance of our FhirParametersSourceGenerator incremental source generator
        var generator = new FhirParametersSourceGenerator();

        // The GeneratorDriver is used to run our generator against a compilation
        Driver = CSharpGeneratorDriver.Create(generator);

    }

    [Benchmark]
    public void GenerateSourceCode()
    {
        // Run the source generator!
        Driver.RunGenerators(Compilation);
    }
}

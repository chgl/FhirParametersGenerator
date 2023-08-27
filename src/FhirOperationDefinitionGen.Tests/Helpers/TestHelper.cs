using System.Collections.Immutable;
using FhirOperationDefinitionGen;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace FhirParametersGenerator.Tests.Helpers;

public static class TestHelper
{
    public class CustomAdditionalText : AdditionalText
    {
        private readonly string _text;

        public override string Path { get; }

        public CustomAdditionalText(string path)
        {
            Path = path;
            _text = File.ReadAllText(path);
        }

        public override SourceText GetText(
            CancellationToken cancellationToken = new CancellationToken()
        )
        {
            return SourceText.From(_text);
        }
    }

    public static Task Verify(IEnumerable<string> additionalTextPaths)
    {
        var references = AppDomain.CurrentDomain
            .GetAssemblies()
            .Where(_ => !_.IsDynamic && !string.IsNullOrWhiteSpace(_.Location))
            .Select(_ => MetadataReference.CreateFromFile(_.Location))
            .Concat(
                new[]
                {
                    MetadataReference.CreateFromFile(
                        typeof(OperationDefinitionParametersSourceGenerator).Assembly.Location
                    ),
                }
            );

        List<AdditionalText> additionalTexts = new List<AdditionalText>();
        foreach (string additionalTextPath in additionalTextPaths)
        {
            AdditionalText additionalText = new CustomAdditionalText(additionalTextPath);
            additionalTexts.Add(additionalText);
        }

        // Create a Roslyn compilation for the syntax tree.
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "SnapshotTests",
            syntaxTrees: null,
            references: references,
            options: new(OutputKind.DynamicallyLinkedLibrary)
        );

        // Create an instance of our OperationDefinitionParametersSourceGenerator incremental source generator
        var generator = new OperationDefinitionParametersSourceGenerator();

        // The GeneratorDriver is used to run our generator against a compilation
        GeneratorDriver driver = CSharpGeneratorDriver
            .Create(generator)
            .AddAdditionalTexts(ImmutableArray.CreateRange(additionalTexts));

        // Run the source generator!
        driver = driver.RunGenerators(compilation);

        DerivePathInfo(
            (_, projectDirectory, type, method) =>
                new(
                    directory: Path.Combine(projectDirectory, "Snapshots"),
                    typeName: TypeNameToAbbreviation(type),
                    methodName: method.Name
                )
        );

        // Use verify to snapshot test the source generator output!
        return Verifier.Verify(driver);
    }

    private static string TypeNameToAbbreviation(Type type)
    {
        return string.Concat(type.Name.Where(c => char.IsUpper(c)));
    }
}

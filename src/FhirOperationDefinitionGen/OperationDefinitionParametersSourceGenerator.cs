using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using System.Text.Json;
using System.Reflection;
using Scriban;

namespace FhirOperationDefinitionGen;

/// <summary>
/// Source generator for generating FHIR Parameters resources from OperationDefinition JSON files.
/// </summary>
[Generator]
public class OperationDefinitionParametersSourceGenerator : IIncrementalGenerator
{
    private static readonly JsonSerializerOptions fhirJsonSerializerOptions =
        new JsonSerializerOptions().ForFhir(ModelInfo.ModelInspector);

    /// <summary>
    /// Initialize
    /// </summary>
    /// <param name="context">The context</param>
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var files = context.AdditionalTextsProvider
            .Where(a =>
            {
                var fileName = Path.GetFileName(a.Path);
                return fileName.StartsWith("OperationDefinition") && fileName.EndsWith(".json");
            })
            .Select((a, c) => (Path.GetFileNameWithoutExtension(a.Path), a.GetText(c)!.ToString()));

        var compilationAndFiles = context.CompilationProvider.Combine(files.Collect());

        context.RegisterSourceOutput(compilationAndFiles, Execute);
    }

    void Execute(
        SourceProductionContext context,
        (Compilation compilation, ImmutableArray<(string, string)> files) compilationAndFiles
    )
    {
        var template = Template.Parse(GetEmbeddedResourceContent("OperationTemplate.sbn"));

        // find anything that matches our files
        foreach (var (path, text) in compilationAndFiles.files)
        {
            var operationDefinition = JsonSerializer.Deserialize<OperationDefinition>(
                text,
                fhirJsonSerializerOptions
            );
            if (operationDefinition is null)
            {
                continue;
            }

            var output = template.Render(
                new { OperationDefinition = operationDefinition },
                member => member.Name
            );
            context.AddSource($"{operationDefinition.Name}Operation.g.cs", output);
        }
    }

    private static string GetEmbeddedResourceContent(string relativePath)
    {
        var baseName = Assembly.GetExecutingAssembly().GetName().Name;
        var resourceName = relativePath
            .TrimStart('.')
            .Replace(Path.DirectorySeparatorChar, '.')
            .Replace(Path.AltDirectorySeparatorChar, '.');

        using var stream =
            Assembly.GetExecutingAssembly().GetManifestResourceStream($"{baseName}.{resourceName}")
            ?? throw new InvalidOperationException(
                "Failed to read operation template from resources."
            );

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

using System.Collections.Generic;
using FhirParametersGenerator;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

// ── Example 1: flat model ────────────────────────────────────────────────────
var flat = new TestModelA
{
    Age = 123,
    Name = "Test",
    WriteOnly = "something",
    Code = new("http://snomed.info/sct", "386661006", "Fever"),
    Patient = new()
    {
        BirthDate = "2000-01-01",
        Deceased = new FhirBoolean(false),
        Name = new()
        {
            new HumanName() { Given = new[] { "Test" }, Family = "User" },
        },
    },
};

Console.WriteLine("=== Flat model ===");
var flatParameters = flat.ToFhirParameters();
Console.WriteLine(flatParameters.ToJson(new() { Pretty = true }));

Console.WriteLine("\n=== Flat model (round-tripped back from Parameters) ===");
var flatRoundTripped = TestModelA.FromFhirParameters(flatParameters);
Console.WriteLine(flatRoundTripped.ToFhirParameters().ToJson(new() { Pretty = true }));

// ── Example 2: list of complex objects ──────────────────────────────────────
var withRules = new ModelWithRules
{
    Rules = new List<FhirPathRule>
    {
        new() { Path = "Patient.name", Method = "redact" },
        new() { Path = "Patient.birthDate", Method = "dateShift" },
    },
};

Console.WriteLine("\n=== List of complex objects ===");
var withRulesParameters = withRules.ToFhirParameters();
Console.WriteLine(withRulesParameters.ToJson(new() { Pretty = true }));

Console.WriteLine("\n=== List of complex objects (round-tripped back from Parameters) ===");
var withRulesRoundTripped = ModelWithRules.FromFhirParameters(withRulesParameters);
Console.WriteLine(withRulesRoundTripped.ToFhirParameters().ToJson(new() { Pretty = true }));

// ── Example 3: nested model (AnonymizerConfiguration-style) ─────────────────
var nested = new AnonymizerStyleConfig
{
    FhirVersion = "R4",
    Settings = new AnonymizerSettings
    {
        DateShiftKey = "my-secret-key",
        CryptoHashKey = "hash-key",
        EnablePartialDatesForRedact = true,
        RestrictedAreas = new List<string>
        {
            "036",
            "692",
            "878",
            "059",
            "790",
            "879",
            "063",
            "821",
            "884",
            "102",
            "823",
            "890",
        },
    },
};

Console.WriteLine("\n=== Nested model ===");
var nestedParameters = nested.ToFhirParameters();
Console.WriteLine(nestedParameters.ToJson(new() { Pretty = true }));

Console.WriteLine("\n=== Nested model (round-tripped back from Parameters) ===");
var nestedRoundTripped = AnonymizerStyleConfig.FromFhirParameters(nestedParameters);
Console.WriteLine(nestedRoundTripped.ToFhirParameters().ToJson(new() { Pretty = true }));

[GenerateFhirParameters]
public partial class TestModelA
{
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; } = 0;
    public bool IsSomething { get; set; } = false;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string WriteOnly
    {
        set { Console.WriteLine($"Write-only: {value}"); }
    }
    public DayOfWeek DayOfWeek { get; init; } = DayOfWeek.Friday;
    public CodeableConcept? Code { get; init; }
    public Patient? Patient { get; init; }
}

[GenerateFhirParameters]
public partial class ModelWithRules
{
    public List<FhirPathRule> Rules { get; init; } = new();
}

public class FhirPathRule
{
    public string Path { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
}

[GenerateFhirParameters]
public partial class AnonymizerStyleConfig
{
    public string FhirVersion { get; init; } = string.Empty;
    public AnonymizerSettings? Settings { get; init; }
}

public class AnonymizerSettings
{
    public string DateShiftKey { get; init; } = string.Empty;
    public string CryptoHashKey { get; init; } = string.Empty;
    public bool EnablePartialDatesForRedact { get; init; }
    public bool EnablePartialAgesForRedact { get; init; }
    public List<string>? RestrictedAreas { get; init; }
}

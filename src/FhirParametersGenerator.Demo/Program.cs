using FhirParametersGenerator;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

var t = new TestModelA
{
    Age = 123,
    Name = "Test",
    WriteOnly = "something",
    Code = new("http://snomed.info/sct", "386661006", "Fever"),
    Patient = new()
    {
        BirthDate = "2000-01-01",
        Deceased = new FhirBoolean(false),
        Name = new() { new HumanName() { Given = new[] { "Test" }, Family = "User" } },
    },
};

var parameters = t.ToFhirParameters();

Console.WriteLine(parameters.ToJson(new() { Pretty = true }));

[GenerateFhirParameters]
public class TestModelA
{
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; } = 0;
    public bool IsSomething { get; set; } = false;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string WriteOnly
    {
        set
        {
            Console.WriteLine($"Write-only: {value}");
        }
    }
    public DayOfWeek DayOfWeek { get; init; } = DayOfWeek.Friday;
    public CodeableConcept? Code { get; init; }
    public Patient? Patient { get; init; }
}

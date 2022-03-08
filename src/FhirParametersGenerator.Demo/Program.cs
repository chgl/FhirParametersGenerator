using FhirParametersGenerator;
using Hl7.Fhir.Serialization;

var t = new TestModelA
{
    Age = 123,
    Name = "Test",
    WriteOnly = "something"
};

var asParameters = t.AsFhirParameters();

Console.WriteLine(asParameters.ToJson(new() { Pretty = true }));

[GenerateFhirParameters]
public class TestModelA
{
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; } = 0;
    public bool IsSomething { get; set; } = false;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public DateTime Time { get; set; } = DateTime.UtcNow;
    public string WriteOnly
    {
        set
        {
            Console.WriteLine($"Write-only: {value}");
        }
    }
    public DayOfWeek DayOfWeek { get; init; } = DayOfWeek.Friday;
}

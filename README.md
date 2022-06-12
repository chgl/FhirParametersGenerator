# FhirParametersGenerator

<p align="center"><img width="100" src="icon.png" alt="FhirParametersGenerator Logo"></p>

[![Build Status](https://github.com/chgl/FhirParametersGenerator/workflows/ci/badge.svg?branch=master)](https://github.com/chgl/FhirParametersGenerator/actions) [![NuGet](https://img.shields.io/nuget/v/FhirParametersGenerator.svg)](https://www.nuget.org/packages/FhirParametersGenerator/)

A PoC .NET source generator for mapping model classes to HL7 FHIR® Parameters resources.

Useful when interacting with FHIR® server operation endpoints.

## Getting Started

```sh
dotnet add package FhirParametersGenerator
```

This sample code...

```cs
using FhirParametersGenerator;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;

var t = new TestModel
{
    Age = 123,
    Name = "Test",
    Code = new("http://snomed.info/sct", "386661006", "Fever"),
};

// the ToFhirParameters() extension method is code-generated
var parameters = t.ToFhirParameters();

Console.WriteLine(parameters.ToJson(new() { Pretty = true }));

// apply this attribute to the desired model class
[GenerateFhirParameters]
public class TestModel
{
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; } = 0;
    public CodeableConcept? Code { get; init; }
}
```

...will output:

```json
{
  "resourceType": "Parameters",
  "parameter": [
    {
      "name": "name",
      "valueString": "Test"
    },
    {
      "name": "age",
      "valueDecimal": 123
    },
    {
      "name": "code",
      "valueCodeableConcept": {
        "coding": [
          {
            "system": "http://snomed.info/sct",
            "code": "386661006"
          }
        ],
        "text": "Fever"
      }
    }
  ]
}
```

## Limitations

This library is in a very early stage and many edge and not-so-edge cases that are not covered by the generated source.
The list of open issues is a good starting point to see what isn't yet supported. Contributions are of course always welcome.

## Benchmark

A benchmark project exists which measures the performance of generating the source code for a 1000-property class: <https://github.com/chgl/FhirParametersGenerator/blob/master/src/FhirParametersGenerator.Benchmark/Program.cs>.

Benchmarking results over time are available at <https://chgl.github.io/FhirParametersGenerator/dev/bench/>.

## Credits

### Source Code

Contains source code published under the terms of the MIT license originally from <https://github.com/andrewlock/NetEscapades.EnumGenerators/> and from <https://github.com/dotnet/runtime/blob/v6.0.2/src/libraries/System.Text.Json/Common/JsonCamelCaseNamingPolicy.cs>.

### Icon

The package icon is composed of

- Fire by artworkbean from NounProject.com
- Edit by Logan from NounProject.com

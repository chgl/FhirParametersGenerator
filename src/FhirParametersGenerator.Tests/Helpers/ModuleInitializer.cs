using System.Runtime.CompilerServices;

namespace FhirParametersGenerator.Tests.Helpers;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        VerifySourceGenerators.Enable();
    }
}

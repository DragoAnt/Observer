using System.Runtime.CompilerServices;
using PublicApiGenerator;

namespace DragoAnt.Observer.Tests;

public sealed class ApiSurfaceTests
{
    private static readonly ApiGeneratorOptions Options = new()
    {
        ExcludeAttributes =
        [
            "System.Runtime.Versioning.TargetFrameworkAttribute",
            "System.Reflection.AssemblyMetadataAttribute",
            "System.Diagnostics.DebuggableAttribute",
        ],
    };

    [ModuleInitializer]
    internal static void VerifyDefaults() => DiffEngine.DiffRunner.Disabled = true;

    [Fact]
    public Task Abstractions() => Verify(typeof(MaskTag).Assembly.GeneratePublicApi(Options)).UseDirectory("api").UseFileName("DragoAnt.Observer.Abstractions");

    [Fact]
    public Task Core() => Verify(typeof(ValueKind).Assembly.GeneratePublicApi(Options)).UseDirectory("api").UseFileName("DragoAnt.Observer.Core");
}

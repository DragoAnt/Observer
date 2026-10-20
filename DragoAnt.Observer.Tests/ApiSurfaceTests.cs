using System.Runtime.CompilerServices;
using PublicApiGenerator;
using System.Diagnostics.CodeAnalysis;

namespace DragoAnt.Observer.Tests;

public sealed class ApiSurfaceTests
{
    private static readonly Type[] Experimental = [.. typeof(ValueKind).Assembly.GetExportedTypes().Where(t => t.IsDefined(typeof(ExperimentalAttribute), false))];
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
    public Task Core() => Verify(typeof(ValueKind).Assembly.GeneratePublicApi(new ApiGeneratorOptions { ExcludeAttributes = Options.ExcludeAttributes, ExcludeTypes = Experimental })).UseDirectory("api").UseFileName("DragoAnt.Observer.Core");

    [Fact]
    public Task CoreExperimental() => Verify(typeof(ValueKind).Assembly.GeneratePublicApi(new ApiGeneratorOptions { ExcludeAttributes = Options.ExcludeAttributes, IncludeTypes = Experimental, IncludeAssemblyAttributes = false })).UseDirectory("api").UseFileName("DragoAnt.Observer.Core.Experimental");

    [Fact]
    public Task Conformance() => Verify(typeof(DragoAnt.Observer.Conformance.ConformanceKit).Assembly.GeneratePublicApi(Options)).UseDirectory("api").UseFileName("DragoAnt.Observer.Conformance");
}

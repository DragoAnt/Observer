using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DragoAnt.Observer.Tests;

internal static class SnippetCompilation
{
    private const string GlobalUsings = "global using System;\nglobal using System.Collections.Generic;\nglobal using System.Linq;\n";

    private static readonly Lazy<MetadataReference[]> References = new(() =>
    {
        _ = typeof(ValueKind).Assembly;
        var platform = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        var local = Directory.GetFiles(AppContext.BaseDirectory, "DragoAnt.Observer.*.dll");
        return platform.Concat(local)
            .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .Select(g => (MetadataReference)MetadataReference.CreateFromFile(g.First()))
            .ToArray();
    });

    public static Assembly? Compile(string code, OutputKind kind, out string diagnostics,
        CSharpCompilationOptions? options = null)
    {
        var tree = CSharpSyntaxTree.ParseText(GlobalUsings + code, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            "Snippet" + Guid.NewGuid().ToString("N"),
            [tree],
            References.Value,
            options ?? new CSharpCompilationOptions(kind, nullableContextOptions: NullableContextOptions.Enable));
        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        diagnostics = string.Join("\n", result.Diagnostics.Where(d => d.Severity >= DiagnosticSeverity.Warning));
        return result.Success ? Assembly.Load(stream.ToArray()) : null;
    }
}

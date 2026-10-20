using System.Reflection;
using Microsoft.CodeAnalysis;

namespace DragoAnt.Observer.Tests;

/// <summary>
/// Every <c>```csharp</c> block of the README, the package readmes and <c>docs/</c> compiles against the current
/// packages; a block with top-level statements runs and prints the <c>// </c> lines under its last
/// <c>Console.WriteLine</c>. A block after <c>&lt;!-- doc-test: skip --&gt;</c> is a fragment.
/// </summary>
public sealed class DocSnippetTests
{
    private static readonly string[] Documents = ["README.md", "abstractions.package.readme.md", "core.package.readme.md", "comments.md", "hashing.md"];

    public static TheoryData<string, int> Snippets()
    {
        var data = new TheoryData<string, int>();
        foreach (var document in Documents)
        {
            for (var i = 0; i < Extract(document).Count; i++)
            {
                data.Add(document, i);
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Snippets))]
    public void Snippet_CompilesAndPrintsItsOutput(string document, int index)
    {
        var (line, code) = Extract(document)[index];

        var assembly = SnippetCompilation.Compile(code, OutputKind.ConsoleApplication, out var diagnostics);
        if (assembly is null && diagnostics.Contains("CS5001", StringComparison.Ordinal))
        {
            SnippetCompilation.Compile(code, OutputKind.DynamicallyLinkedLibrary, out diagnostics).Should().NotBeNull($"{document} snippet at line {line} must compile:\n{diagnostics}");
            return;
        }

        assembly.Should().NotBeNull($"{document} snippet at line {line} must compile:\n{diagnostics}");
        var printed = Run(assembly!);
        if (ExpectedOutput(code) is { } expected)
        {
            printed.TrimEnd().Should().Be(expected, $"{document} snippet at line {line} prints its output comment");
        }
    }

    private static List<(int Line, string Code)> Extract(string document)
    {
        var lines = File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "docs", document));
        var snippets = new List<(int, string)>();
        for (var i = 0; i < lines.Length; i++)
        {
            if (lines[i].Trim() != "```csharp")
            {
                continue;
            }

            var skip = i > 0 && lines[i - 1].Contains("doc-test: skip", StringComparison.Ordinal);
            var end = i + 1;
            while (end < lines.Length && lines[end].Trim() != "```")
            {
                end++;
            }

            if (!skip)
            {
                snippets.Add((i + 2, string.Join('\n', lines[(i + 1)..end])));
            }

            i = end;
        }

        return snippets;
    }

    private static string? ExpectedOutput(string code)
    {
        var lines = code.Split('\n');
        var last = Array.FindLastIndex(lines, l => l.Contains("Console.WriteLine(", StringComparison.Ordinal));
        if (last < 0)
        {
            return null;
        }

        var output = lines.Skip(last + 1)
            .TakeWhile(l => l.TrimStart().StartsWith("// ", StringComparison.Ordinal) || l.Trim() == "//")
            .Where(l => !l.TrimStart().StartsWith("// language=", StringComparison.Ordinal))
            .Select(l => l.TrimStart().Length > 2 ? l.TrimStart()[3..] : string.Empty)
            .ToList();
        return output.Count == 0 ? null : string.Join('\n', output);
    }

    private static string Run(Assembly assembly)
    {
        var entry = assembly.EntryPoint!;
        var original = Console.Out;
        var printed = new StringWriter();
        Console.SetOut(printed);
        try
        {
            entry.Invoke(null, entry.GetParameters().Length == 0 ? null : [Array.Empty<string>()]);
        }
        finally
        {
            Console.SetOut(original);
        }

        return printed.ToString().Replace("\r\n", "\n", StringComparison.Ordinal);
    }
}

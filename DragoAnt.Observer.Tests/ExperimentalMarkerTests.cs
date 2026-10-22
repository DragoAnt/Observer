using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Reflection;
using DragoAnt.Observer.Conformance;
using DragoAnt.Observer.Formats;
using Microsoft.CodeAnalysis;

namespace DragoAnt.Observer.Tests;

public sealed class ExperimentalMarkerTests
{
    [Theory]
    [InlineData("DAOEXP001", "static class C { static object M() => MaskRules.Create(b => b.Match(\"password\").Mask(MaskTag.Full)); }")]
    [InlineData("DAOEXP002", "static class C { static string M(DragoAnt.Observer.Formats.IObserverFormat format) => format.Name; }")]
    public void ExperimentalApi_FailsTheBuildUntilSuppressed(string id, string code)
    {
        var source = "using DragoAnt.Observer;\n" + code;
        SnippetCompilation.Compile(source, OutputKind.DynamicallyLinkedLibrary, out var diagnostics).Should().BeNull();
        diagnostics.Should().Contain($"error {id}");
        SnippetCompilation.Compile($"#pragma warning disable {id}\n{source}", OutputKind.DynamicallyLinkedLibrary, out diagnostics)
            .Should().NotBeNull(diagnostics);
    }

    [Fact]
    public void ExperimentalTypes_AreExactlyTheFormatSeam()
    {
        var marked = typeof(ValueKind).Assembly.GetExportedTypes()
            .Select(t => (t.FullName, Id: t.GetCustomAttribute<ExperimentalAttribute>()?.DiagnosticId))
            .Where(t => t.Id is not null).OrderBy(t => t.FullName, StringComparer.Ordinal).ToList();
        marked.Should().Equal(
            ("DragoAnt.Observer.Formats.IObserverFormat", "DAOEXP002"),
            ("DragoAnt.Observer.Formats.IObserverFormatBinding", "DAOEXP002"),
            ("DragoAnt.Observer.Formats.MaskRule", "DAOEXP001"),
            ("DragoAnt.Observer.Formats.MaskRuleAction", "DAOEXP001"),
            ("DragoAnt.Observer.Formats.MaskRuleScope", "DAOEXP001"),
            ("DragoAnt.Observer.MaskRuleBuilder", "DAOEXP001"),
            ("DragoAnt.Observer.MaskRules", "DAOEXP001"),
            ("DragoAnt.Observer.MaskRulesBuilder", "DAOEXP001"));
    }

    [Fact]
    public void ConformanceTypes_AreAllExperimental()
    {
        typeof(ConformanceKit).Assembly.GetExportedTypes()
            .Where(t => t.GetCustomAttribute<ExperimentalAttribute>()?.DiagnosticId != "DAOEXP002")
            .Select(t => t.FullName).Should().BeEmpty();
    }

    [Fact]
    public void ObserverFormat_HasTheSyncAndTheAsyncPath()
    {
        var mask = typeof(IObserverFormatBinding).GetMethod(nameof(IObserverFormatBinding.Mask))!;
        mask.ReturnType.Should().Be(typeof(MaskResult));
        mask.GetParameters().Select(p => p.ParameterType).Should().Equal(
            typeof(ReadOnlySpan<byte>), typeof(IBufferWriter<byte>), typeof(ObserverOptions));

        var run = typeof(IObserverFormatBinding).GetMethod(nameof(IObserverFormatBinding.MaskAsync))!;
        run.ReturnType.Should().Be(typeof(ValueTask<MaskRecordsResult>));
        run.GetParameters().Select(p => p.ParameterType).Should().Equal(
            typeof(PipeReader), typeof(PipeWriter), typeof(ObserverOptions), typeof(CancellationToken));

        typeof(IObserverFormat).GetMethod(nameof(IObserverFormat.Bind))!.GetParameters().Single().ParameterType
            .Should().Be(typeof(MaskRules));
    }
}

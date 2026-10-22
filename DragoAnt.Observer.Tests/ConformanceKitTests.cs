using System.Buffers;
using System.IO.Pipelines;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DragoAnt.Observer.Conformance;
using DragoAnt.Observer.Formats;

namespace DragoAnt.Observer.Tests;

public sealed class ConformanceKitTests
{
    [Fact]
    public void Seed_IsWellFormed()
    {
        ConformanceKit.Seed.Should().HaveCountGreaterThanOrEqualTo(10);
        ConformanceKit.Seed.Select(s => s.Name).Should().OnlyHaveUniqueItems();
        var secrets = new List<string>();
        var windows = new List<string>();
        var visibles = new List<string>();
        foreach (var scenario in ConformanceKit.Seed)
        {
            scenario.Documents.Should().ContainKey("json");
            var json = scenario.Documents["json"];
            if (json.Status == MaskStatus.Masked)
            {
                using var _ = JsonDocument.Parse(json.Text,
                    new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
            }
            foreach (var secret in scenario.Secrets)
            {
                secret.Clear.Should().HaveLength(16).And.StartWith("ZQS");
                secret.Clear.Should().MatchRegex("^[A-Za-z0-9]+$");
                scenario.Documents.Values.Should().Contain(d => d.Text.Contains(secret.Clear, StringComparison.Ordinal));
                secrets.Add(secret.Clear);
                windows.AddRange(Enumerable.Range(0, 9).Select(i => secret.Clear.Substring(i, 8)));
            }

            foreach (var visible in scenario.Visible)
            {
                visible.Should().HaveLength(16).And.StartWith("ZQK");
                visible.Should().MatchRegex("^[A-Za-z0-9]+$");
                scenario.Documents.Values.Should().Contain(d => d.Text.Contains(visible, StringComparison.Ordinal));
                visibles.Add(visible);
            }
        }

        secrets.Should().OnlyHaveUniqueItems();
        windows.Should().OnlyHaveUniqueItems();
        windows.Should().NotContain(w => visibles.Any(v => v.Contains(w, StringComparison.Ordinal)));
    }

    [Fact]
    public async Task PlantedFormats_FailOnlyTheInvariantsTheyTarget()
    {
        var clean = ConformanceKit.Seed.Where(s => s.Documents["json"] is { Status: MaskStatus.Masked, Flags: MaskFlags.None }).ToList();
        var untouched = ConformanceKit.Seed.Where(s => s.Name == "untouched-blocklist").ToList();
        var comments = new ConformanceTraits { Comments = true };

        await AssertFailsOnly(new EchoFormat(), comments, clean, "leak", "tag-output");
        await AssertFailsOnly(new StarsFormat(), comments, clean, "visible", "tag-output");
        await AssertFailsOnly(new ThrowingFormat(), comments, null, "no-throw");
        await AssertFailsOnly(new NoDocumentsFormat(), comments, null, "has-documents");
        await AssertFailsOnly(new AsyncDiffersFormat(), comments, untouched, "sync-async");
        await AssertFailsOnly(new UndefinedStatusFormat(), comments, untouched, "defined-status");
        await AssertFailsOnly(new RoundTripDiffersFormat(), new ConformanceTraits { RoundTripsUntouchedInput = true }, untouched, "round-trip");
    }

    [Fact]
    public async Task EchoFormat_PassesWhatItDoesNotTarget()
    {
        var report = await ConformanceKit.RunAsync(new EchoFormat(), new ConformanceTraits(),
            ConformanceKit.Seed.Where(s => s.Name == "untouched-blocklist"));

        report.Failures.Should().BeEmpty();
        report.ScenariosRun.Should().Be(1);
    }

    [Fact]
    public void Seed_ContainsATruncationScenario()
    {
        ConformanceKit.Seed.Should().Contain(s => s.Documents["json"].Status == MaskStatus.Truncated);
    }

    [Fact]
    public async Task TruncationScenario_ChecksLeaksOnTheCappedOutput()
    {
        var scenario = ConformanceKit.Seed.Single(s => s.Name == "truncation");
        var report = await ConformanceKit.RunAsync(new EchoFormat(), new ConformanceTraits(), [scenario]);

        report.Failures.Should().Contain(f => f.Invariant == "leak" && f.Detail.StartsWith("whole document", StringComparison.Ordinal));
    }

    [Fact]
    public async Task SingleSecretPrefixLeak_IdentifiesOnlyThatSecret()
    {
        var scenario = ConformanceKit.Seed.Single(s => s.Name == "path-and-depth");
        var target = scenario.Secrets.Single(s => s.Path == "a.b.token");
        var report = await ConformanceKit.RunAsync(
            new SingleSecretLeakFormat(target.Clear, target.Clear[..8]),
            new ConformanceTraits(),
            [scenario]);

        var failures = report.Failures.Where(f => f.Invariant == "leak").ToList();
        failures.Should().NotBeEmpty();
        failures.All(f => f.Detail.Contains(target.Path, StringComparison.Ordinal)).Should().BeTrue();
        failures.All(f => f.Detail.Contains(target.Clear, StringComparison.Ordinal)).Should().BeTrue();
    }

    [Fact]
    public async Task SingleSecretTailLeak_IdentifiesOnlyThatSecret()
    {
        var scenario = ConformanceKit.Seed.Single(s => s.Name == "path-and-depth");
        var target = scenario.Secrets.Single(s => s.Path == "a.b.token");
        var report = await ConformanceKit.RunAsync(
            new SingleSecretLeakFormat(target.Clear, target.Clear[6..]),
            new ConformanceTraits(),
            [scenario]);

        var failures = report.Failures.Where(f => f.Invariant == "leak").ToList();
        failures.Should().NotBeEmpty();
        failures.All(f => f.Detail.Contains(target.Path, StringComparison.Ordinal)).Should().BeTrue();
        failures.All(f => f.Detail.Contains(target.Clear, StringComparison.Ordinal)).Should().BeTrue();
    }

    [Fact]
    public void HashText_UsesTheDocumentedTextHash()
    {
        const string clear = "ZQS3c9C300000003";
        var key = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
        var expected = "7:" + Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.Unicode.GetBytes(clear))[..16]);
        HashText.For(clear, ConformanceKit.DefaultOptions).Should().Be(expected);
    }

    private static async Task AssertFailsOnly(IObserverFormat format, ConformanceTraits traits,
        IEnumerable<ConformanceScenario>? scenarios, params string[] invariants)
    {
        var report = await ConformanceKit.RunAsync(format, traits, scenarios);
        report.Failures.Select(f => f.Invariant).Distinct().Should().BeEquivalentTo(invariants,
            $"{format.GetType().Name}: {string.Join("; ", report.Failures.Take(5))}");
    }

    private abstract class FakeFormat(string name) : IObserverFormat, IObserverFormatBinding
    {
        public string Name { get; } = name;
        public IReadOnlyList<string> MediaTypes { get; } = [];
        public IObserverFormatBinding Bind(MaskRules rules) => this;
        public abstract MaskResult Mask(ReadOnlySpan<byte> input, IBufferWriter<byte> output, ObserverOptions? options = null);
        public virtual async ValueTask<MaskRecordsResult> MaskAsync(PipeReader input, PipeWriter output, ObserverOptions? options = null, CancellationToken cancellationToken = default)
        {
            var read = await input.ReadAsync(cancellationToken);
            while (!read.IsCompleted)
            {
                input.AdvanceTo(read.Buffer.Start, read.Buffer.End);
                read = await input.ReadAsync(cancellationToken);
            }

            var buffer = read.Buffer.ToArray();
            input.AdvanceTo(read.Buffer.End);
            var result = Mask(buffer, output, options);
            await output.FlushAsync(cancellationToken);
            return MaskRecordsResult.FromDocument(result, buffer.Length);
        }

        protected static MaskResult Write(ReadOnlySpan<byte> bytes, IBufferWriter<byte> output, MaskStatus status = MaskStatus.Masked)
        {
            output.Write(bytes);
            return new MaskResult { Status = status, BytesWritten = bytes.Length, FailedAtByte = -1 };
        }
    }

    private sealed class EchoFormat() : FakeFormat("json")
    {
        public override MaskResult Mask(ReadOnlySpan<byte> input, IBufferWriter<byte> output, ObserverOptions? options = null) => Write(input, output);
    }

    private sealed class SingleSecretLeakFormat(string clear, string leaked) : FakeFormat("json")
    {
        private readonly byte[] _clear = Encoding.UTF8.GetBytes(clear);
        private readonly byte[] _leaked = Encoding.UTF8.GetBytes(leaked);

        public override MaskResult Mask(ReadOnlySpan<byte> input, IBufferWriter<byte> output, ObserverOptions? options = null)
            => input.IndexOf(_clear) >= 0 ? Write(_leaked, output) : Write([], output);
    }

    private sealed class StarsFormat() : FakeFormat("json")
    {
        public override MaskResult Mask(ReadOnlySpan<byte> input, IBufferWriter<byte> output, ObserverOptions? options = null) => Write("***"u8, output);
    }

    private sealed class ThrowingFormat() : FakeFormat("json")
    {
        public override MaskResult Mask(ReadOnlySpan<byte> input, IBufferWriter<byte> output, ObserverOptions? options = null) => throw new FormatException("planted");
    }

    private sealed class NoDocumentsFormat() : FakeFormat("none")
    {
        public override MaskResult Mask(ReadOnlySpan<byte> input, IBufferWriter<byte> output, ObserverOptions? options = null) => Write(input, output);
    }

    private sealed class AsyncDiffersFormat() : FakeFormat("json")
    {
        public override MaskResult Mask(ReadOnlySpan<byte> input, IBufferWriter<byte> output, ObserverOptions? options = null) => Write(input, output);

        public override async ValueTask<MaskRecordsResult> MaskAsync(PipeReader input, PipeWriter output, ObserverOptions? options = null, CancellationToken cancellationToken = default)
        {
            var read = await input.ReadAsync(cancellationToken);
            var length = read.Buffer.Length;
            input.AdvanceTo(read.Buffer.End);
            output.Write("y"u8);
            await output.FlushAsync(cancellationToken);
            return MaskRecordsResult.FromDocument(new MaskResult { Status = MaskStatus.Masked, BytesWritten = 1, FailedAtByte = -1 }, length);
        }
    }

    private sealed class UndefinedStatusFormat() : FakeFormat("json")
    {
        public override MaskResult Mask(ReadOnlySpan<byte> input, IBufferWriter<byte> output, ObserverOptions? options = null)
            => input.EndsWith("}"u8) ? Write(input, output) : Write(input, output) with { Status = (MaskStatus)int.MaxValue };
    }

    private sealed class RoundTripDiffersFormat() : FakeFormat("json")
    {
        public override MaskResult Mask(ReadOnlySpan<byte> input, IBufferWriter<byte> output, ObserverOptions? options = null)
            => Write([.. input, (byte)' '], output);
    }
}

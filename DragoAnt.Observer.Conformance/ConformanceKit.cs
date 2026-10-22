using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text;
using DragoAnt.Observer.Formats;

namespace DragoAnt.Observer.Conformance;

/// <summary>Format-neutral scenarios and invariants for every observer format.</summary>
[Experimental("DAOEXP002", UrlFormat = "https://github.com/DragoAnt/Observer/blob/main/docs/experimental.md")]
public static class ConformanceKit
{
    private const int LeakWindowBytes = 8;

    /// <summary>Stable options used by the seed, including a fixed hash key and key id.</summary>
    public static ObserverOptions DefaultOptions { get; } = new()
    {
        HashKey = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray(),
        HashKeyId = 7,
    };

    /// <summary>The shared seed scenarios.</summary>
    public static IReadOnlyList<ConformanceScenario> Seed { get; } = ConformanceSeed.Create();

    /// <summary>The stable invariant identifiers, in evaluation order.</summary>
    public static IReadOnlyList<string> Invariants { get; } =
        ["leak", "no-throw", "status", "defined-status", "visible", "tag-output", "round-trip", "sync-async", "has-documents"];

    /// <summary>Masks every selected document and every prefix, returning every invariant failure instead of throwing.</summary>
    /// <param name="format">The format to exercise.</param>
    /// <param name="traits">Promises made by the format.</param>
    /// <param name="scenarios">Scenarios to run; the seed when omitted.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The failures and counts.</returns>
    public static async Task<ConformanceReport> RunAsync(IObserverFormat format, ConformanceTraits traits,
        IEnumerable<ConformanceScenario>? scenarios = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(format);
        ArgumentNullException.ThrowIfNull(traits);
        var failures = new List<ConformanceFailure>();
        var selected = (scenarios ?? Seed)
            .Where(s => s.Documents.ContainsKey(format.Name) && (!s.NeedsComments || traits.Comments)).ToList();
        var checks = 0;

        if (selected.Count == 0)
        {
            failures.Add(new ConformanceFailure("run", format.Name, "has-documents", "No selected scenario has a document for this format."));
            return new ConformanceReport(failures, 0, 1);
        }

        checks++;
        foreach (var scenario in selected)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var document = scenario.Documents[format.Name];
            var input = Encoding.UTF8.GetBytes(document.Text);
            IObserverFormatBinding binding;
            try
            {
                binding = format.Bind(scenario.Rules);
            }
            catch (Exception exception)
            {
                failures.Add(Failure(scenario, format, "no-throw", $"Bind threw {exception.GetType().Name}: {exception.Message}"));
                checks++;
                continue;
            }

            if (!TryMask(binding, input, scenario.Options, out var wholeOutput, out var wholeResult, out var error))
            {
                failures.Add(Failure(scenario, format, "no-throw", $"whole document: {error}"));
                checks++;
            }
            else
            {
                checks += CheckWhole(scenario, format, traits, document, input, wholeOutput, wholeResult, failures);
                if (!traits.MultiRecord)
                {
                    checks++;
                    await CheckAsync(binding, scenario, format, input, wholeOutput, wholeResult, failures, cancellationToken)
                        .ConfigureAwait(false);
                }
            }

            for (var cut = 0; cut < input.Length; cut++)
            {
                checks++;
                if (!TryMask(binding, input.AsSpan(0, cut), scenario.Options, out var output, out var result, out error))
                {
                    failures.Add(Failure(scenario, format, "no-throw", $"prefix {cut} of {input.Length}: {error}"));
                    continue;
                }

                checks++;
                if (!Enum.IsDefined(result.Status))
                    failures.Add(Failure(scenario, format, "defined-status", $"prefix {cut} of {input.Length}: {(int)result.Status}"));
                checks += CheckLeaks(scenario, format, output, failures, $"prefix {cut} of {input.Length}");
            }
        }

        return new ConformanceReport(failures, selected.Count, checks);
    }

    private static int CheckWhole(ConformanceScenario scenario, IObserverFormat format, ConformanceTraits traits,
        ConformanceDocument document, byte[] input, byte[] output, MaskResult result, List<ConformanceFailure> failures)
    {
        var checks = CheckLeaks(scenario, format, output, failures, "whole document");
        checks++;
        if (result.Status != document.Status || result.Flags != document.Flags)
            failures.Add(Failure(scenario, format, "status", $"Expected {document.Status}/{document.Flags}; got {result.Status}/{result.Flags}."));

        foreach (var visible in scenario.Visible)
        {
            checks++;
            var expected = Encoding.UTF8.GetBytes(traits.EncodeValue(visible));
            if (output.AsSpan().IndexOf(expected) < 0)
                failures.Add(Failure(scenario, format, "visible", $"Output does not contain {visible}."));
        }

        foreach (var secret in scenario.Secrets.Where(s => s.Tag.Kind != MaskKind.Null))
        {
            checks++;
            var replacement = secret.Tag.Kind switch
            {
                MaskKind.Full => "***",
                MaskKind.Last4 => "***" + secret.Clear[^4..],
                MaskKind.Hash => HashText.For(secret.Clear, scenario.Options),
                _ => null,
            };
            if (replacement is not null && output.AsSpan().IndexOf(Encoding.UTF8.GetBytes(traits.EncodeValue(replacement))) < 0)
                failures.Add(Failure(scenario, format, "tag-output", $"Output lacks {secret.Tag.Kind} replacement for {secret.Path}."));
        }

        if (traits.RoundTripsUntouchedInput && scenario.Name.StartsWith("untouched-", StringComparison.Ordinal))
        {
            checks++;
            if (!output.AsSpan().SequenceEqual(input))
                failures.Add(Failure(scenario, format, "round-trip", "Untouched input changed."));
        }

        return checks;
    }

    private static int CheckLeaks(ConformanceScenario scenario, IObserverFormat format, byte[] output,
        List<ConformanceFailure> failures, string where)
    {
        var checks = 0;
        foreach (var secret in scenario.Secrets)
        {
            checks++;
            var clear = Encoding.UTF8.GetBytes(secret.Clear);
            var window = Math.Min(LeakWindowBytes, clear.Length);
            for (var start = 0; window > 0 && start + window <= clear.Length; start++)
            {
                if (output.AsSpan().IndexOf(clear.AsSpan(start, window)) >= 0)
                {
                    failures.Add(Failure(scenario, format, "leak", $"{where}: output contains {window} bytes of {secret.Path} ({secret.Clear}) at {start}."));
                    break;
                }
            }
        }

        return checks;
    }

    private static async Task CheckAsync(IObserverFormatBinding binding, ConformanceScenario scenario,
        IObserverFormat format, byte[] input, byte[] syncOutput, MaskResult syncResult,
        List<ConformanceFailure> failures, CancellationToken cancellationToken)
    {
        try
        {
            await using var inputStream = new MemoryStream(input);
            await using var outputStream = new MemoryStream();
            var reader = PipeReader.Create(inputStream, new StreamPipeReaderOptions(bufferSize: 1, minimumReadSize: 1));
            var writer = PipeWriter.Create(outputStream, new StreamPipeWriterOptions(leaveOpen: true));
            try
            {
                var result = await binding.MaskAsync(reader, writer, scenario.Options, cancellationToken).ConfigureAwait(false);
                if (!outputStream.ToArray().AsSpan().SequenceEqual(syncOutput) || result != MaskRecordsResult.FromDocument(syncResult, input.Length))
                    failures.Add(Failure(scenario, format, "sync-async", "Async bytes or result differ from the sync path."));
            }
            finally
            {
                await reader.CompleteAsync().ConfigureAwait(false);
                await writer.CompleteAsync().ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            failures.Add(Failure(scenario, format, "sync-async", $"Async path threw {exception.GetType().Name}: {exception.Message}"));
        }
    }

    private static bool TryMask(IObserverFormatBinding binding, ReadOnlySpan<byte> input, ObserverOptions options,
        out byte[] output, out MaskResult result, out string? error)
    {
        try
        {
            var writer = new ArrayBufferWriter<byte>();
            result = binding.Mask(input, writer, options);
            output = writer.WrittenSpan.ToArray();
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            result = default;
            output = [];
            error = $"{exception.GetType().Name}: {exception.Message}";
            return false;
        }
    }

    private static ConformanceFailure Failure(ConformanceScenario scenario, IObserverFormat format,
        string invariant, string detail) => new(scenario.Name, format.Name, invariant, detail.Length <= 120 ? detail : detail[..120]);
}

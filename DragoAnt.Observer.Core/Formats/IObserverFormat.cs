using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;

namespace DragoAnt.Observer.Formats;

/// <summary>
/// A format an observer masks, found by its name — a query string or a <c>.env</c> file has no media type — or by a
/// media type when it has one.
/// </summary>
[Experimental(ExperimentalIds.Formats, UrlFormat = ExperimentalIds.Url)]
public interface IObserverFormat
{
    /// <summary>The format's name in lower case, for example <c>json</c>, <c>ndjson</c>, <c>form</c> or <c>env</c>.</summary>
    string Name { get; }

    /// <summary>The media types of the format, for example <c>application/json</c>; empty when it has none.</summary>
    IReadOnlyList<string> MediaTypes { get; }

    /// <summary>Binds format-neutral rules to the format. Bind once and share the binding: it is thread-safe.</summary>
    /// <param name="rules">The rules.</param>
    /// <returns>The rules, ready to mask this format.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rules"/> is <c>null</c>.</exception>
    IObserverFormatBinding Bind(MaskRules rules);
}

/// <summary>
/// Format-neutral rules bound to one format. Never throws for bad input: problems are reported in the result, and only
/// masked values are ever written.
/// </summary>
[Experimental(ExperimentalIds.Formats, UrlFormat = ExperimentalIds.Url)]
public interface IObserverFormatBinding
{
    /// <summary>Masks one document held in memory into <paramref name="output"/>.</summary>
    /// <param name="input">The document; it may be cut short, for example by a size limit.</param>
    /// <param name="output">Receives the masked document.</param>
    /// <param name="options">The call's options; a format's own options type adds its settings, and plain
    /// <see cref="ObserverOptions"/> get the format's defaults for them.</param>
    /// <returns>Status, bytes written and the input offset where reading stopped.</returns>
    MaskResult Mask(ReadOnlySpan<byte> input, IBufferWriter<byte> output, ObserverOptions? options = null);

    /// <summary>
    /// Masks <paramref name="input"/> to its end into <paramref name="output"/>, record by record in a format with many
    /// records per input; a format with one document per input masks a run of one. The reader and the writer are left
    /// open for the caller to complete.
    /// </summary>
    /// <param name="input">The input.</param>
    /// <param name="output">Receives the masked output; flushed before the call returns.</param>
    /// <param name="options">The call's options, as for <see cref="Mask"/>.</param>
    /// <param name="cancellationToken">Cancels reading and writing.</param>
    /// <returns>The run's worst status, its flags, record counters and byte counts.</returns>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled, or a pending read was cancelled.</exception>
    ValueTask<MaskRecordsResult> MaskAsync(PipeReader input, PipeWriter output, ObserverOptions? options = null,
        CancellationToken cancellationToken = default);
}

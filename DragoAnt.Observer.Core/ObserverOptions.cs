namespace DragoAnt.Observer;

/// <summary>
/// Limits and settings of one observer call, the same for every format. Every limit applies per document; in a format
/// with many records per input, each record is a document. A format adds its own output settings in a derived record.
/// </summary>
public record ObserverOptions
{
    /// <summary>
    /// Defaults: no size limits but <see cref="MaxRecordBytes"/>, depth 64, a per-process hash key, the built-in strategy,
    /// names matched ignoring case and no-value members kept.
    /// </summary>
    public static ObserverOptions Default { get; } = new();

    /// <summary>
    /// Output size limit of one document, in bytes; when reached the output is closed and the result has <see cref="MaskFlags.OutputCapped"/>.
    /// </summary>
    public int MaxOutputBytes { get; init; } = int.MaxValue;

    /// <summary>
    /// Longest string value written unmasked, in UTF-8 bytes; a longer one is cut, ends with an ellipsis and sets
    /// <see cref="MaskFlags.ValueCut"/>. A strategy receives the whole value up to <see cref="MaxMaskValueBytes"/>, and mask output — a hash
    /// included — is never cut.
    /// </summary>
    public int MaxValueBytes { get; init; } = int.MaxValue;

    /// <summary>
    /// Deepest nesting of one document; a deeper document is <see cref="MaskStatus.Invalid"/> with <see cref="MaskFlags.Depth"/>.
    /// </summary>
    public int MaxDepth { get; init; } = 64;

    /// <summary>
    /// Largest input of one record, in bytes, for a format that reads many records from one input, such as NDJSON or
    /// logfmt; a longer record is never written raw: it is counted as failed and masked whole or dropped, as the format
    /// documents. Formats that read one document per input ignore it. 1 MiB by default.
    /// </summary>
    public int MaxRecordBytes { get; init; } = 1 << 20;

    /// <summary>
    /// Longest value handed to a <see cref="ValueMaskStrategy"/>, in bytes after the format transcodes it to UTF-8; a
    /// longer value is written as <c>***</c> without calling the strategy and sets <see cref="MaskFlags.ValueTooLarge"/>.
    /// No limit by default; a format that cannot hold a value whole documents a lower one.
    /// </summary>
    public int MaxMaskValueBytes { get; init; } = int.MaxValue;

    /// <summary>
    /// Key of <see cref="MaskKind.Hash"/>. When empty, a random key is used for the lifetime of the process, so hashes
    /// correlate within one process only. The bytes of a Microsoft <c>HmacRedactorOptions.Key</c> (a base64 string)
    /// give the same hashes; see <see cref="ObserverOptionsExtensions.WithBase64HashKey{TOptions}"/>.
    /// </summary>
    public ReadOnlyMemory<byte> HashKey { get; init; }

    /// <summary>
    /// Identifier of <see cref="HashKey"/>, written before every hash as <c>"&lt;id&gt;:"</c> so that a reader knows
    /// which key produced it; <c>null</c> writes the hash alone. The same as <c>HmacRedactorOptions.KeyId</c>.
    /// </summary>
    public int? HashKeyId { get; init; }

    /// <summary>
    /// Strategy for rules that mask with a <see cref="MaskTag"/>; <see cref="ValueMaskStrategy.Default"/> when <c>null</c>.
    /// </summary>
    public ValueMaskStrategy? Strategy { get; init; }

    /// <summary>
    /// Match rule names, name tests and shape members ignoring case, as by default. Pass the case option of the
    /// format's serializer to match names the way deserialization does. With <c>false</c> a rule no longer catches a
    /// differently cased name: under a block list such a value is written unchanged, under an allow list it is masked.
    /// </summary>
    public bool NameCaseInsensitive { get; init; } = true;

    /// <summary>
    /// Drop members and array items whose value is the format's no-value form — <c>null</c> in JSON, an unquoted empty
    /// value such as <c>a=</c> in text formats — and objects and arrays left empty by that.
    /// </summary>
    public bool IgnoreNulls { get; init; }

    /// <summary>
    /// What happens to comments in the input that no comment rule decides; <see cref="CommentPolicy.AllowList"/> by
    /// default, which drops every comment a rule does not keep.
    /// </summary>
    public CommentPolicy Comments { get; init; } = CommentPolicy.AllowList;
}

/// <summary>
/// Helpers for <see cref="ObserverOptions"/> that keep the derived options type.
/// </summary>
public static class ObserverOptionsExtensions
{
    /// <summary>
    /// The minimum length of a base64 hash key, the same rule as Microsoft's <c>HmacRedactorOptions</c> validator.
    /// </summary>
    public const int MinBase64HashKeyLength = 44;

    /// <summary>
    /// A copy of <paramref name="options"/> whose <see cref="ObserverOptions.HashKey"/> is the decoded
    /// <paramref name="base64Key"/>: pass the <c>Key</c> of a Microsoft <c>HmacRedactorOptions</c> to get the same hashes.
    /// </summary>
    /// <param name="options">Options to copy.</param>
    /// <param name="base64Key">The key as a base64 string of at least <see cref="MinBase64HashKeyLength"/> characters.</param>
    /// <typeparam name="TOptions">Type of the options, kept by the copy.</typeparam>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> or <paramref name="base64Key"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="base64Key"/> is shorter than the minimum or not base64.</exception>
    public static TOptions WithBase64HashKey<TOptions>(this TOptions options, string base64Key)
        where TOptions : ObserverOptions
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(base64Key);
        if (base64Key.Length < MinBase64HashKeyLength)
        {
            throw new ArgumentException($"A hash key needs at least {MinBase64HashKeyLength} base64 characters.", nameof(base64Key));
        }

        var buffer = new byte[base64Key.Length * 3 / 4];
        if (!Convert.TryFromBase64String(base64Key, buffer, out var written))
        {
            throw new ArgumentException("The hash key is not a base64 string.", nameof(base64Key));
        }

        var copy = (ObserverOptions)options with { HashKey = buffer.AsMemory(0, written) };
        return (TOptions)copy;
    }
}

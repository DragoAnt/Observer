namespace DragoAnt.Observer;

/// <summary>
/// Limits and settings of one observer call, the same for every format. A format adds its own output settings in a
/// derived record, such as the JSON observer's <c>JsonObserverOptions</c>.
/// </summary>
public record ObserverOptions
{
    /// <summary>
    /// Defaults: no size limits, depth 64, a per-process hash key, the built-in strategy, names matched ignoring case
    /// and <c>null</c> values kept.
    /// </summary>
    public static ObserverOptions Default { get; } = new();

    /// <summary>
    /// Output size limit in bytes; when reached the output is closed and the result has <see cref="MaskFlags.OutputCapped"/>.
    /// </summary>
    public int MaxOutputBytes { get; init; } = int.MaxValue;

    /// <summary>
    /// Longest string value written unmasked, in UTF-8 bytes; a longer one is cut, ends with an ellipsis and sets
    /// <see cref="MaskFlags.ValueCut"/>. A masking function receives the whole value, and mask output — a hash
    /// included — is never cut.
    /// </summary>
    public int MaxValueBytes { get; init; } = int.MaxValue;

    /// <summary>
    /// Deepest nesting accepted; a deeper payload is <see cref="MaskStatus.Invalid"/> with <see cref="MaskFlags.Depth"/>.
    /// </summary>
    public int MaxDepth { get; init; } = 64;

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
    /// Drop members and array items whose value is <c>null</c>, and objects and arrays left empty by that.
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

namespace DragoAnt.Observer;

/// <summary>
/// Outcome of masking or reading a payload. Whatever the status, the output never holds a value a rule masks.
/// </summary>
public enum MaskStatus
{
    /// <summary>
    /// The whole payload was masked and every value was written in full; <see cref="MaskResult.Flags"/> is empty.
    /// </summary>
    Masked,

    /// <summary>
    /// The output is valid but not everything came through intact; <see cref="MaskResult.Flags"/> says why: the input
    /// ended inside the document, the output reached its size limit, a value was cut, data followed the document, or
    /// invalid text was replaced.
    /// </summary>
    Truncated,

    /// <summary>
    /// The payload is empty or not in the expected format at all, for example JSON whose root is not an object or an
    /// array. Nothing was written.
    /// </summary>
    Unrecognized,

    /// <summary>
    /// The payload is not valid, or a rule failed. The output holds the masked part read before the failure, closed so
    /// that it stays well-formed; nothing after the failure is written.
    /// </summary>
    Invalid,
}

/// <summary>
/// Details of a result that is not a clean <see cref="MaskStatus.Masked"/>; several can be set at once.
/// </summary>
[Flags]
public enum MaskFlags
{
    /// <summary>
    /// Nothing to report.
    /// </summary>
    None = 0,

    /// <summary>
    /// The input ended inside the document, for example a body cut by a size limit.
    /// </summary>
    InputTruncated = 1,

    /// <summary>
    /// The output reached <see cref="ObserverOptions.MaxOutputBytes"/>; the rest was not written.
    /// </summary>
    OutputCapped = 2,

    /// <summary>
    /// At least one value written unmasked was longer than <see cref="ObserverOptions.MaxValueBytes"/> and was cut.
    /// </summary>
    ValueCut = 4,

    /// <summary>
    /// More data followed the end of the document; it was not read or written.
    /// </summary>
    TrailingData = 8,

    /// <summary>
    /// A name or value held invalid UTF-8, which was written as the replacement character U+FFFD.
    /// </summary>
    InvalidUtf8Replaced = 16,

    /// <summary>
    /// The document is nested deeper than <see cref="ObserverOptions.MaxDepth"/>.
    /// </summary>
    Depth = 32,
}

/// <summary>
/// Result of masking or reading a payload.
/// </summary>
public readonly record struct MaskResult
{
    /// <summary>
    /// What happened.
    /// </summary>
    public MaskStatus Status { get; init; }

    /// <summary>
    /// Bytes written to the output; 0 when only reading.
    /// </summary>
    public int BytesWritten { get; init; }

    /// <summary>
    /// Offset in the input where reading stopped; -1 when the whole payload was read, 0 for
    /// <see cref="MaskStatus.Unrecognized"/>.
    /// </summary>
    public long FailedAtByte { get; init; }

    /// <summary>
    /// Why the result is not a clean <see cref="MaskStatus.Masked"/>; <see cref="MaskFlags.None"/> when it is.
    /// </summary>
    public MaskFlags Flags { get; init; }
}

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
    /// ended inside the document, the output reached its size limit, a value was cut or too large for the strategy,
    /// data followed the document, or invalid text was replaced.
    /// </summary>
    Truncated,

    /// <summary>
    /// Nothing could be read as this format: the input is empty where a document is required, or its first token cannot
    /// start one — for example JSON whose root is a string or a number. Nothing was written.
    /// </summary>
    Unrecognized,

    /// <summary>
    /// The input breaks the format's grammar or the shape the observer expects — for example a root array given to an
    /// observer of objects — or a rule failed. The output holds the masked part read before the failure, closed so that
    /// it stays well-formed; nothing after the failure is written. In a format with many records per input this is one
    /// record's status, and the run goes on (<see cref="MaskRecordsResult"/>).
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

    /// <summary>
    /// A value was longer than <see cref="ObserverOptions.MaxMaskValueBytes"/>; it was written as <c>***</c> without
    /// calling the strategy.
    /// </summary>
    ValueTooLarge = 64,

    /// <summary>
    /// A record of a run failed — its status was <see cref="MaskStatus.Invalid"/> or <see cref="MaskStatus.Unrecognized"/>
    /// — and was masked whole or dropped, never written raw; the run went on. Set on <see cref="MaskRecordsResult.Flags"/> only.
    /// </summary>
    RecordFailed = 128,
}

/// <summary>
/// Result of masking or reading one document; a record of a format with many records per input is one document, and
/// <see cref="MaskRecordsResult"/> sums a run of them.
/// </summary>
public readonly record struct MaskResult
{
    /// <summary>
    /// What happened.
    /// </summary>
    public MaskStatus Status { get; init; }

    /// <summary>
    /// Bytes written to the output for this document; 0 when only reading.
    /// </summary>
    public long BytesWritten { get; init; }

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

/// <summary>
/// Result of masking a run of records — the lines of an NDJSON or logfmt input — or of one document read from a stream,
/// which is a run of one. A failed record never ends the run.
/// </summary>
public readonly record struct MaskRecordsResult
{
    /// <summary>
    /// The worst status of any record, in the order <see cref="MaskStatus.Masked"/>,
    /// <see cref="MaskStatus.Truncated"/>, <see cref="MaskStatus.Unrecognized"/>,
    /// <see cref="MaskStatus.Invalid"/>.
    /// </summary>
    public MaskStatus Status { get; init; }

    /// <summary>The flags of every record together.</summary>
    public MaskFlags Flags { get; init; }

    /// <summary>Records read, failed ones included.</summary>
    public long Records { get; init; }

    /// <summary>Records whose status was invalid or unrecognized.</summary>
    public long FailedRecords { get; init; }

    /// <summary>Failed records left out of the output.</summary>
    public long DroppedRecords { get; init; }

    /// <summary>Position of the first failed record, from 0; -1 when none failed.</summary>
    public long FirstFailedRecord { get; init; }

    /// <summary>Bytes read from the input.</summary>
    public long BytesRead { get; init; }

    /// <summary>Bytes written to the output.</summary>
    public long BytesWritten { get; init; }

    /// <summary>Creates the run result for one document.</summary>
    /// <param name="document">Result of the document.</param>
    /// <param name="bytesRead">Bytes read from the input.</param>
    public static MaskRecordsResult FromDocument(in MaskResult document, long bytesRead)
    {
        var failed = document.Status is MaskStatus.Invalid or MaskStatus.Unrecognized;
        return new MaskRecordsResult
        {
            Status = document.Status,
            Flags = failed ? document.Flags | MaskFlags.RecordFailed : document.Flags,
            Records = 1,
            FailedRecords = failed ? 1 : 0,
            FirstFailedRecord = failed ? 0 : -1,
            BytesRead = bytesRead,
            BytesWritten = document.BytesWritten,
        };
    }
}

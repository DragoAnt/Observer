namespace DragoAnt.Observer;

/// <summary>
/// How a sensitive value is masked.
/// </summary>
public enum MaskKind
{
    /// <summary>
    /// Replaced by <c>"***"</c>.
    /// </summary>
    Full,

    /// <summary>
    /// Only the last four characters are kept, as <c>"***1234"</c>; values shorter than eight characters are masked fully.
    /// </summary>
    Last4,

    /// <summary>
    /// Replaced by a keyed hash, so that equal values can be correlated without being shown. The text is the same as the
    /// <c>HmacRedactor</c> of Microsoft.Extensions.Compliance.Redaction writes for the same key.
    /// </summary>
    Hash,

    /// <summary>
    /// Replaced by <c>null</c>; the member itself stays.
    /// </summary>
    Null,

    /// <summary>
    /// Masked the way a custom strategy decides from <see cref="MaskTag.Key"/>; the built-in strategy replaces it by <c>"***"</c>.
    /// </summary>
    Custom,
}

namespace DragoAnt.Observer;

/// <summary>
/// Type of a value, the same for every format.
/// </summary>
public enum ValueKind : byte
{
    /// <summary>
    /// A string.
    /// </summary>
    String,

    /// <summary>
    /// A number.
    /// </summary>
    Number,

    /// <summary>
    /// <c>true</c> or <c>false</c>.
    /// </summary>
    Boolean,

    /// <summary>
    /// No value — the format's no-value form: <c>null</c> in JSON, an empty cell in CSV, an unquoted empty value such
    /// as <c>a=</c> in text formats. A quoted empty value is an empty <see cref="String"/>.
    /// </summary>
    Null,

    /// <summary>
    /// An object: members with names.
    /// </summary>
    Object,

    /// <summary>
    /// An array: items with positions.
    /// </summary>
    Array,

    /// <summary>
    /// An opaque payload, such as a file part: <see cref="MaskContext.Value"/> holds its raw bytes. The built-in strategy
    /// hashes the bytes for <see cref="MaskKind.Hash"/>, writes the no-value form for <see cref="MaskKind.Null"/>, and
    /// <c>***</c> for every other tag.
    /// </summary>
    Binary,
}

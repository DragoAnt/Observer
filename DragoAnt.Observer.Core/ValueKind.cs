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
    /// No value: <c>null</c> in JSON, an empty cell in CSV.
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
}

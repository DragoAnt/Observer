using System.ComponentModel;

namespace DragoAnt.Observer;

/// <summary>
/// What a <see cref="ValueMaskStrategy"/> knows about the value it masks. Valid only during the call it is passed to;
/// reading it allocates nothing.
/// </summary>
public readonly ref struct MaskContext
{
    private readonly DataPath _path;

    /// <summary>
    /// Creates the context of one value. For format implementations.
    /// </summary>
    /// <param name="value">The unescaped text of a string, the literal of a number or boolean, or empty.</param>
    /// <param name="kind">Type of the value.</param>
    /// <param name="tag">How the rule asks for the value to be masked.</param>
    /// <param name="options">Options of the current call.</param>
    /// <param name="path">Path of the value.</param>
    /// <param name="valueIndex">Position of the value among the values masked in this call, from 0.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public MaskContext(ReadOnlySpan<byte> value, ValueKind kind, MaskTag tag, ObserverOptions options, in DataPath path, long valueIndex)
    {
        Value = value;
        Kind = kind;
        Tag = tag;
        Options = options ?? ObserverOptions.Default;
        _path = path;
        ValueIndex = valueIndex;
    }

    /// <summary>
    /// The unescaped UTF-8 text of a string, the literal of a number or boolean (<c>12.50</c>, <c>true</c>), or empty
    /// for <c>null</c>, an object or an array — always the whole value, whatever <see cref="ObserverOptions.MaxValueBytes"/> says.
    /// </summary>
    public ReadOnlySpan<byte> Value { get; }

    /// <summary>
    /// Type of the value; <see cref="ValueKind.Object"/> or <see cref="ValueKind.Array"/> for a container masked whole.
    /// </summary>
    public ValueKind Kind { get; }

    /// <summary>
    /// How the rule asks for the value to be masked.
    /// </summary>
    public MaskTag Tag { get; }

    /// <summary>
    /// Options of the current call; a format passes its derived options type.
    /// </summary>
    public ObserverOptions Options { get; }

    /// <summary>
    /// Path of the value from the root, array items included.
    /// </summary>
    public DataPath Path => _path;

    /// <summary>
    /// Unescaped UTF-8 name of the member that holds the value; empty for an array item.
    /// </summary>
    public ReadOnlySpan<byte> Name => _path.LastName;

    /// <summary>
    /// The value is an item of an array rather than the value of a member.
    /// </summary>
    public bool IsArrayItem => _path.IsItem(_path.Length - 1);

    /// <summary>
    /// Position of the value among the values handed to the strategy in this call, from 0, in document order.
    /// </summary>
    public long ValueIndex { get; }
}

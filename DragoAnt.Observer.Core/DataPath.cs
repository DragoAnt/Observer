using System.Buffers;
using System.ComponentModel;
using System.Text;

namespace DragoAnt.Observer;

/// <summary>
/// Path of the value an observer is at: one level per enclosing member or array item, from the root down.
/// </summary>
/// <remarks>
/// Valid only during the call it is passed to. Names are kept as unescaped UTF-8 and decoded only when asked for; an
/// item keeps its position, so <see cref="ToString"/> renders <c>items[2].sku</c>. The members marked as editor-hidden
/// build the path while a format reads its input; rules and strategies only read it.
/// </remarks>
public ref struct DataPath
{
    private readonly ReadOnlySpan<byte> _input;
    private Segment[] _segments;
    private byte[]? _scratch;
    private int _scratchUsed;
    private int _count;

    /// <summary>
    /// Creates an empty path for one pass over <paramref name="input"/>; dispose it when the pass ends. For format implementations.
    /// </summary>
    /// <param name="input">The input whose names <see cref="PushInputName"/> can point into; empty when the input is not one span.</param>
    /// <param name="capacity">Levels to reserve, for example <see cref="MaxLength"/> of an earlier pass.</param>
    /// <param name="nameCaseInsensitive">Whether name tests ignore case in this pass, see <see cref="ObserverOptions.NameCaseInsensitive"/>.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public DataPath(ReadOnlySpan<byte> input, int capacity, bool nameCaseInsensitive)
    {
        _input = input;
        _segments = ArrayPool<Segment>.Shared.Rent(Math.Max(capacity, 1));
        NameCaseInsensitive = nameCaseInsensitive;
    }

    /// <summary>
    /// Number of levels in the path.
    /// </summary>
    public readonly int Length => _count;

    private readonly int Depth => _count - 1;

    /// <summary>
    /// The deepest <see cref="Length"/> this path reached, so that a later pass can reserve enough levels. For format implementations.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public int MaxLength { readonly get; private set; }

    internal readonly bool NameCaseInsensitive { get; }

    /// <summary>
    /// Unescaped UTF-8 name of the deepest level; empty for an array item or an empty path.
    /// </summary>
    public readonly ReadOnlySpan<byte> LastName => TryGetName(Depth, out var name) ? name : default;

    /// <summary>
    /// Adds a level for a name that stands unescaped in the input given to the constructor. For format implementations.
    /// </summary>
    /// <param name="start">Offset of the name in the input.</param>
    /// <param name="length">Length of the name in bytes.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void PushInputName(int start, int length)
    {
        if (_input.IsEmpty || (uint)start > (uint)_input.Length || (uint)length > (uint)(_input.Length - start))
        {
            throw new ArgumentOutOfRangeException(nameof(start));
        }

        ref var segment = ref Push();
        segment = new Segment(SegmentKind.Input, start, length);
    }

    /// <summary>
    /// Adds a level for an unescaped UTF-8 name, copying it. For format implementations.
    /// </summary>
    /// <param name="utf8Name">The name.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void PushName(ReadOnlySpan<byte> utf8Name)
    {
        utf8Name.CopyTo(ReserveName(utf8Name.Length));
        PushReservedName(utf8Name.Length);
    }

    /// <summary>
    /// Room for a name of up to <paramref name="maxLength"/> bytes, to decode into before <see cref="PushReservedName"/>.
    /// For format implementations.
    /// </summary>
    /// <param name="maxLength">Largest length the decoded name can have.</param>
    /// <returns>The room; valid until the next change of the path.</returns>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public Span<byte> ReserveName(int maxLength)
    {
        var start = _scratchUsed;
        var required = start + maxLength;
        if (_scratch is null || required > _scratch.Length)
        {
            var grown = ArrayPool<byte>.Shared.Rent(Math.Max(required, 256));
            if (_scratch is not null)
            {
                _scratch.AsSpan(0, start).CopyTo(grown);
                ArrayPool<byte>.Shared.Return(_scratch);
            }

            _scratch = grown;
        }

        return _scratch.AsSpan(start, maxLength);
    }

    /// <summary>
    /// Adds a level for the name just decoded into the room <see cref="ReserveName"/> returned. For format implementations.
    /// </summary>
    /// <param name="length">Bytes of the name written into the room.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void PushReservedName(int length)
    {
        var start = _scratchUsed;
        if (_scratch is null || (uint)length > (uint)(_scratch.Length - start))
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        ref var segment = ref Push();
        segment = new Segment(SegmentKind.Scratch, start, length);
        _scratchUsed = start + length;
    }

    /// <summary>
    /// Adds a level for the array item at <paramref name="index"/>. For format implementations.
    /// </summary>
    /// <param name="index">Zero-based position of the item in its array.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void PushItem(int index)
    {
        ref var segment = ref Push();
        segment = new Segment(SegmentKind.Item, index, 0);
    }

    /// <summary>
    /// Removes the deepest level. For format implementations.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Pop()
    {
        if (_count == 0)
        {
            return;
        }

        ref var segment = ref _segments[--_count];
        if (segment.Kind == SegmentKind.Scratch)
        {
            _scratchUsed = segment.Start;
        }

        segment = default;
    }

    /// <summary>
    /// Gets the unescaped UTF-8 name of a level without decoding it.
    /// </summary>
    /// <param name="index">Level, from 0 to <see cref="Length"/> - 1.</param>
    /// <param name="utf8Name">The name; valid only during the call.</param>
    /// <returns><c>false</c> for an array item or an index out of range.</returns>
    public readonly bool TryGetName(int index, out ReadOnlySpan<byte> utf8Name)
    {
        if (index < 0 || index > Depth)
        {
            utf8Name = default;
            return false;
        }

        var segment = _segments[index];
        switch (segment.Kind)
        {
            case SegmentKind.Input:
                utf8Name = _input.Slice(segment.Start, segment.Length);
                return true;
            case SegmentKind.Scratch:
                utf8Name = _scratch.AsSpan(segment.Start, segment.Length);
                return true;
            default:
                utf8Name = default;
                return false;
        }
    }

    /// <summary>
    /// Whether the level at <paramref name="index"/> is an array item.
    /// </summary>
    /// <param name="index">Level, from 0 to <see cref="Length"/> - 1.</param>
    public readonly bool IsItem(int index) => index >= 0 && index <= Depth && _segments[index].Kind == SegmentKind.Item;

    /// <summary>
    /// Gets the zero-based position of an array item level within its array.
    /// </summary>
    /// <param name="index">Level, from 0 to <see cref="Length"/> - 1.</param>
    /// <param name="itemIndex">Position of the item; -1 when the level is not an array item.</param>
    /// <returns><c>true</c> when the level is an array item.</returns>
    public readonly bool TryGetItemIndex(int index, out int itemIndex)
    {
        if (IsItem(index))
        {
            itemIndex = _segments[index].Start;
            return true;
        }

        itemIndex = -1;
        return false;
    }

    /// <summary>
    /// Name of the level at <paramref name="index"/>, 0 being the root's member.
    /// </summary>
    /// <param name="index">Level, from 0 to <see cref="Length"/> - 1.</param>
    /// <returns>The decoded name; <c>null</c> for an array item or an index out of range.</returns>
    public readonly string? GetName(int index)
    {
        if (index < 0 || index > Depth)
        {
            return null;
        }

        ref var segment = ref _segments[index];
        if (segment.Decoded is not null || segment.Kind == SegmentKind.Item)
        {
            return segment.Decoded;
        }

        TryGetName(index, out var utf8);
        return segment.Decoded = Encoding.UTF8.GetString(utf8);
    }

    /// <summary>
    /// Name of a level counted from the deepest one.
    /// </summary>
    /// <param name="fromEnd">0 for the value's own name, 1 for its parent, and so on.</param>
    /// <returns>The decoded name; <c>null</c> for an array item or an index out of range.</returns>
    public readonly string? GetNameFromEnd(int fromEnd) => GetName(Depth - fromEnd);

    /// <summary>
    /// The path from the root down: names joined with dots and array items as <c>[index]</c>, for example
    /// <c>items[2].sku</c>; a name that is empty or holds <c>.</c>, <c>[</c>, <c>]</c> or <c>'</c> is written as <c>['name']</c>.
    /// </summary>
    public override readonly string ToString()
    {
        var text = new StringBuilder();
        for (var i = 0; i <= Depth; i++)
        {
            if (TryGetItemIndex(i, out var itemIndex))
            {
                text.Append('[').Append(itemIndex).Append(']');
                continue;
            }

            AppendName(text, GetName(i)!, first: i == 0);
        }

        return text.ToString();
    }

    /// <summary>
    /// Writes <see cref="ToString"/>'s text into <paramref name="destination"/> without allocating.
    /// </summary>
    /// <param name="destination">Receives the text.</param>
    /// <param name="charsWritten">Characters written; 0 when the text does not fit.</param>
    /// <returns><c>false</c> when <paramref name="destination"/> is too short.</returns>
    public readonly bool TryFormat(Span<char> destination, out int charsWritten)
    {
        var written = 0;
        for (var i = 0; i <= Depth; i++)
        {
            if (TryGetItemIndex(i, out var itemIndex))
            {
                if (!TryAppend(destination, ref written, '[') || !itemIndex.TryFormat(destination[written..], out var digits))
                {
                    charsWritten = 0;
                    return false;
                }

                written += digits;
                if (!TryAppend(destination, ref written, ']'))
                {
                    charsWritten = 0;
                    return false;
                }

                continue;
            }

            TryGetName(i, out var utf8);
            if (!TryAppendName(destination, ref written, utf8, first: i == 0))
            {
                charsWritten = 0;
                return false;
            }
        }

        charsWritten = written;
        return true;
    }

    /// <summary>
    /// Returns the pooled buffers. For format implementations; the path must not be used afterwards.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Dispose()
    {
        var segments = _segments;
        _segments = [];
        if (segments is { Length: > 0 })
        {
            ArrayPool<Segment>.Shared.Return(segments, clearArray: true);
        }

        var scratch = _scratch;
        _scratch = null;
        if (scratch is not null)
        {
            ArrayPool<byte>.Shared.Return(scratch);
        }
    }

    internal static void AppendName(StringBuilder text, string name, bool first)
    {
        if (NeedsBrackets(name))
        {
            text.Append("['").Append(name.Replace("'", "\\'", StringComparison.Ordinal)).Append("']");
            return;
        }

        if (!first)
        {
            text.Append('.');
        }

        text.Append(name);
    }

    internal static bool NeedsBrackets(ReadOnlySpan<char> name) => name.Length == 0 || name.IndexOfAny(".[]'") >= 0;

    private static bool TryAppendName(Span<char> destination, ref int written, ReadOnlySpan<byte> utf8, bool first)
    {
        var maxChars = Encoding.UTF8.GetMaxCharCount(utf8.Length);
        char[]? rented = null;
        var chars = maxChars <= 256 ? stackalloc char[256] : rented = ArrayPool<char>.Shared.Rent(maxChars);
        try
        {
            var name = chars[..Encoding.UTF8.GetChars(utf8, chars)];
            if (!NeedsBrackets(name))
            {
                return (first || TryAppend(destination, ref written, '.')) && TryAppend(destination, ref written, name);
            }

            if (!TryAppend(destination, ref written, "['"))
            {
                return false;
            }

            foreach (var c in name)
            {
                if ((c == '\'' && !TryAppend(destination, ref written, '\\')) || !TryAppend(destination, ref written, c))
                {
                    return false;
                }
            }

            return TryAppend(destination, ref written, "']");
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }

    private static bool TryAppend(Span<char> destination, ref int written, char c)
    {
        if (written >= destination.Length)
        {
            return false;
        }

        destination[written++] = c;
        return true;
    }

    private static bool TryAppend(Span<char> destination, ref int written, ReadOnlySpan<char> text)
    {
        if (!text.TryCopyTo(destination[written..]))
        {
            return false;
        }

        written += text.Length;
        return true;
    }

    private ref Segment Push()
    {
        var depth = _count++;
        MaxLength = Math.Max(MaxLength, _count);

        if (depth >= _segments.Length)
        {
            var grown = ArrayPool<Segment>.Shared.Rent(Math.Max(MaxLength, _segments.Length * 2));
            _segments.AsSpan().CopyTo(grown);
            if (_segments.Length > 0)
            {
                ArrayPool<Segment>.Shared.Return(_segments, clearArray: true);
            }

            _segments = grown;
        }

        return ref _segments[depth];
    }

    private enum SegmentKind : byte
    {
        Item,
        Input,
        Scratch,
    }

    private struct Segment(SegmentKind kind, int start, int length)
    {
        public readonly SegmentKind Kind = kind;
        public readonly int Start = start;
        public readonly int Length = length;
        public string? Decoded;
    }
}

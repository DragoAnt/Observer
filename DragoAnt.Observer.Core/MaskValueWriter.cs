namespace DragoAnt.Observer;

/// <summary>
/// Where a <see cref="ValueMaskStrategy"/> writes the replacement of one value; each format implements it with its own
/// encoding (JSON escaping, CSV quoting, …). A strategy writes exactly one value per call.
/// </summary>
public abstract class MaskValueWriter
{
    private static ReadOnlySpan<byte> Stars => "***"u8;

    /// <summary>
    /// Creates a writer. For format implementations.
    /// </summary>
    protected MaskValueWriter()
    {
    }

    /// <summary>
    /// Writes a string given as unescaped UTF-8 text.
    /// </summary>
    /// <param name="utf8">The text.</param>
    public abstract void String(ReadOnlySpan<byte> utf8);

    /// <summary>
    /// Writes a string given as UTF-16 text, for example the output of a char-based redactor.
    /// </summary>
    /// <param name="chars">The text.</param>
    public abstract void String(ReadOnlySpan<char> chars);

    /// <summary>
    /// Writes a number given as its literal, for example <c>12.50</c>. A literal that is not a valid number is written
    /// as <c>"***"</c> and fails the call, so the output never holds a malformed value.
    /// </summary>
    /// <param name="utf8Literal">The literal: an optional minus, digits, an optional fraction and exponent.</param>
    public void Number(ReadOnlySpan<byte> utf8Literal)
    {
        if (NumberLiteral.IsValid(utf8Literal))
        {
            WriteNumber(utf8Literal);
            return;
        }

        String(Stars);
        InvalidNumber();
    }

    /// <summary>
    /// Writes <c>true</c> or <c>false</c>.
    /// </summary>
    /// <param name="value">The value.</param>
    public abstract void Boolean(bool value);

    /// <summary>
    /// Writes no value: <c>null</c> in JSON, an empty cell in CSV.
    /// </summary>
    public abstract void Null();

    /// <summary>
    /// Writes the original value unchanged.
    /// </summary>
    public abstract void Keep();

    /// <summary>
    /// Writes a comment next to the value, for formats that have comments; the others ignore it.
    /// </summary>
    /// <param name="text">The comment text, without comment markers.</param>
    public virtual void Comment(ReadOnlySpan<char> text)
    {
    }

    /// <summary>
    /// Writes a number literal that <see cref="Number"/> has checked.
    /// </summary>
    /// <param name="utf8Literal">A valid number literal.</param>
    protected abstract void WriteNumber(ReadOnlySpan<byte> utf8Literal);

    /// <summary>
    /// Called after <see cref="Number"/> wrote <c>"***"</c> for an invalid literal; a format fails the call here.
    /// </summary>
    protected virtual void InvalidNumber()
    {
    }
}

/// <summary>
/// The number grammar shared by the formats: <c>-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][+-]?[0-9]+)?</c>.
/// </summary>
internal static class NumberLiteral
{
    public static bool IsValid(ReadOnlySpan<byte> literal)
    {
        var i = 0;
        if (i < literal.Length && literal[i] == (byte)'-')
        {
            i++;
        }

        if (i >= literal.Length)
        {
            return false;
        }

        if (literal[i] == (byte)'0')
        {
            i++;
        }
        else if (IsDigit(literal[i]))
        {
            while (i < literal.Length && IsDigit(literal[i]))
            {
                i++;
            }
        }
        else
        {
            return false;
        }

        if (i < literal.Length && literal[i] == (byte)'.')
        {
            i++;
            if (!Digits(literal, ref i))
            {
                return false;
            }
        }

        if (i < literal.Length && literal[i] is (byte)'e' or (byte)'E')
        {
            i++;
            if (i < literal.Length && literal[i] is (byte)'+' or (byte)'-')
            {
                i++;
            }

            if (!Digits(literal, ref i))
            {
                return false;
            }
        }

        return i == literal.Length;
    }

    private static bool Digits(ReadOnlySpan<byte> literal, ref int i)
    {
        var start = i;
        while (i < literal.Length && IsDigit(literal[i]))
        {
            i++;
        }

        return i > start;
    }

    private static bool IsDigit(byte b) => (uint)(b - '0') <= 9;
}

using System.Text;

namespace DragoAnt.Observer.Tests;

/// <summary>
/// Records what a strategy writes as one line of text: <c>"x"</c>, <c>12</c>, <c>true</c>, <c>null</c>, <c>keep</c>.
/// </summary>
internal sealed class RecordingWriter : MaskValueWriter
{
    private readonly StringBuilder _text = new();

    public bool Failed { get; private set; }

    public string Text => _text.ToString();

    public override void String(ReadOnlySpan<byte> utf8) => _text.Append('"').Append(Encoding.UTF8.GetString(utf8)).Append('"');

    public override void String(ReadOnlySpan<char> chars) => _text.Append('"').Append(chars).Append('"');

    public override void Boolean(bool value) => _text.Append(value ? "true" : "false");

    public override void Null() => _text.Append("null");

    public override void Keep() => _text.Append("keep");

    public override void Comment(ReadOnlySpan<char> text) => _text.Append("/*").Append(text).Append("*/");

    protected override void WriteNumber(ReadOnlySpan<byte> utf8Literal) => _text.Append(Encoding.UTF8.GetString(utf8Literal));

    protected override void InvalidNumber() => Failed = true;

    public static string Mask(ValueMaskStrategy strategy, string value, ValueKind kind, MaskTag tag, ObserverOptions? options = null)
    {
        var writer = new RecordingWriter();
        var path = new DataPath(default, 4, true);
        try
        {
            path.PushName("field"u8);
            strategy.Mask(new MaskContext(Encoding.UTF8.GetBytes(value), kind, tag, options ?? ObserverOptions.Default, in path, 0), writer);
        }
        finally
        {
            path.Dispose();
        }

        return writer.Text;
    }
}

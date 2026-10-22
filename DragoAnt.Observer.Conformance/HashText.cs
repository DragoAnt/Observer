using System.Text;

namespace DragoAnt.Observer.Conformance;

internal static class HashText
{
    public static string For(string clear, ObserverOptions options)
    {
        var path = new DataPath(default, 1, options.NameCaseInsensitive);
        try
        {
            path.PushName("value"u8);
            var writer = new CaptureWriter();
            ValueMaskStrategy.Default.Mask(
                new MaskContext(Encoding.UTF8.GetBytes(clear), ValueKind.String, MaskTag.Hash, options, in path, 0), writer);
            return writer.Value;
        }
        finally
        {
            path.Dispose();
        }
    }

    private sealed class CaptureWriter : MaskValueWriter
    {
        public string Value { get; private set; } = "";
        public override void String(ReadOnlySpan<byte> utf8) => Value = Encoding.UTF8.GetString(utf8);
        public override void String(ReadOnlySpan<char> chars) => Value = chars.ToString();
        public override void Boolean(bool value) => Value = value ? "true" : "false";
        public override void Null() => Value = "";
        public override void Keep() => throw new InvalidOperationException();
        protected override void WriteNumber(ReadOnlySpan<byte> utf8Literal) => String(utf8Literal);
    }
}

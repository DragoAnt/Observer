using System.Text;

namespace DragoAnt.Observer.Tests;

public sealed class StrategyTests
{
    [Theory]
    [InlineData("4111111111111111", ValueKind.String, MaskKind.Full, "\"***\"")]
    [InlineData("4111111111111111", ValueKind.String, MaskKind.Last4, "\"***1111\"")]
    [InlineData("1234567", ValueKind.String, MaskKind.Last4, "\"***\"")]
    [InlineData("12345678", ValueKind.Number, MaskKind.Last4, "\"***5678\"")]
    [InlineData("пароль-пароль", ValueKind.String, MaskKind.Last4, "\"***роль\"")]
    [InlineData("true", ValueKind.Boolean, MaskKind.Last4, "\"***\"")]
    [InlineData("x", ValueKind.String, MaskKind.Null, "null")]
    [InlineData("", ValueKind.Object, MaskKind.Null, "null")]
    [InlineData("", ValueKind.Object, MaskKind.Full, "\"***\"")]
    [InlineData("", ValueKind.Array, MaskKind.Last4, "\"***\"")]
    [InlineData("x", ValueKind.String, MaskKind.Custom, "\"***\"")]
    public void Default_MasksEveryKind(string value, ValueKind kind, MaskKind mask, string expected)
        => RecordingWriter.Mask(ValueMaskStrategy.Default, value, kind, MaskTag.Create(mask, mask == MaskKind.Custom ? "k" : null))
            .Should().Be(expected);

    [Fact]
    public void CustomStrategy_SeesTheContext()
    {
        var strategy = new SpyStrategy();
        var writer = new RecordingWriter();
        var path = new DataPath(default, 2, true);
        try
        {
            path.PushName("card"u8);
            path.PushItem(3);
            strategy.Mask(new MaskContext("4111"u8, ValueKind.Number, MaskTag.Custom("pan"), ObserverOptions.Default, in path, 7), writer);
        }
        finally
        {
            path.Dispose();
        }

        strategy.Seen.Should().Be("4111|Number|Custom (pan)|card[3]||True|7");
        writer.Text.Should().Be("4111");
    }

    [Theory]
    [InlineData("0", true)]
    [InlineData("-0", true)]
    [InlineData("12.50", true)]
    [InlineData("1e400", true)]
    [InlineData("-1.5E-3", true)]
    [InlineData("", false)]
    [InlineData("-", false)]
    [InlineData("01", false)]
    [InlineData("1.", false)]
    [InlineData(".5", false)]
    [InlineData("1e", false)]
    [InlineData("1e+", false)]
    [InlineData("NaN", false)]
    [InlineData("12a", false)]
    [InlineData("+1", false)]
    public void Number_IsValidated(string literal, bool valid)
    {
        var writer = new RecordingWriter();

        writer.Number(Encoding.UTF8.GetBytes(literal));

        writer.Failed.Should().Be(!valid);
        writer.Text.Should().Be(valid ? literal : "\"***\"");
    }

    [Fact]
    public void Comment_IsIgnoredByDefault()
    {
        var writer = new NoCommentWriter();

        writer.Comment("x");

        writer.Text.Should().BeEmpty();
    }

    private sealed class SpyStrategy : ValueMaskStrategy
    {
        public string Seen { get; private set; } = "";

        public override void Mask(in MaskContext context, MaskValueWriter output)
        {
            Seen = $"{Encoding.UTF8.GetString(context.Value)}|{context.Kind}|{context.Tag}|{context.Path.ToString()}|{Encoding.UTF8.GetString(context.Name)}|{context.IsArrayItem}|{context.ValueIndex}";
            output.Number(context.Value);
        }
    }

    private sealed class NoCommentWriter : MaskValueWriter
    {
        public string Text { get; private set; } = "";

        public override void String(ReadOnlySpan<byte> utf8) => Text += "s";

        public override void String(ReadOnlySpan<char> chars) => Text += "s";

        public override void Boolean(bool value) => Text += "b";

        public override void Null() => Text += "n";

        public override void Keep() => Text += "k";

        protected override void WriteNumber(ReadOnlySpan<byte> utf8Literal) => Text += "#";
    }
}

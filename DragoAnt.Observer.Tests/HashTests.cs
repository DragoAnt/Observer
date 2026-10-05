using System.Text;
using Microsoft.Extensions.Compliance.Redaction;
using Microsoft.Extensions.Options;

namespace DragoAnt.Observer.Tests;

public sealed class HashTests
{
    private static readonly string Base64Key = Convert.ToBase64String(Enumerable.Range(0, 64).Select(i => (byte)(i * 7 + 3)).ToArray());

    public static TheoryData<string, ValueKind> Corpus => new()
    {
        { "alice@example.com", ValueKind.String },
        { "4111111111111111", ValueKind.String },
        { "Grüße, Jürgen — 東京", ValueKind.String },
        { "emoji 😀 pair 𝄞", ValueKind.String },
        { "a", ValueKind.String },
        { "12.50", ValueKind.Number },
        { "-0", ValueKind.Number },
        { "1e400", ValueKind.Number },
        { "true", ValueKind.Boolean },
        { "false", ValueKind.Boolean },
        { new string('x', 300), ValueKind.String },
        { string.Concat(Enumerable.Repeat("ж😀", 200)), ValueKind.String },
    };

    [Theory]
    [MemberData(nameof(Corpus))]
    public void Hash_EqualsHmacRedactor(string value, ValueKind kind)
    {
        var redactor = Redactor(keyId: null);
        var options = ObserverOptions.Default.WithBase64HashKey(Base64Key);

        RecordingWriter.Mask(ValueMaskStrategy.Default, value, kind, MaskTag.Hash, options)
            .Should().Be($"\"{redactor.Redact(value)}\"");
    }

    [Theory]
    [MemberData(nameof(Corpus))]
    public void Hash_WithKeyId_EqualsHmacRedactor(string value, ValueKind kind)
    {
        var redactor = Redactor(keyId: 42);
        var options = ObserverOptions.Default.WithBase64HashKey(Base64Key) with { HashKeyId = 42 };

        RecordingWriter.Mask(ValueMaskStrategy.Default, value, kind, MaskTag.Hash, options)
            .Should().Be($"\"{redactor.Redact(value)}\"").And.StartWith("\"42:");
    }

    [Fact]
    public void Hash_OfAnEmptyString_IsEmpty()
    {
        Redactor(keyId: 7).Redact("").Should().BeEmpty();
        RecordingWriter.Mask(ValueMaskStrategy.Default, "", ValueKind.String, MaskTag.Hash, ObserverOptions.Default with { HashKeyId = 7 })
            .Should().Be("\"\"");
    }

    [Fact]
    public void Hash_IgnoresKindAndTagKey()
    {
        var options = ObserverOptions.Default.WithBase64HashKey(Base64Key);

        var number = RecordingWriter.Mask(ValueMaskStrategy.Default, "1", ValueKind.Number, MaskTag.Hash, options);
        var text = RecordingWriter.Mask(ValueMaskStrategy.Default, "1", ValueKind.String, MaskTag.Create(MaskKind.Hash, "pii"), options);

        number.Should().Be(text).And.HaveLength(26);
    }

    [Fact]
    public void Hash_WithoutAKey_IsStableWithinTheProcess()
    {
        var first = RecordingWriter.Mask(ValueMaskStrategy.Default, "x", ValueKind.String, MaskTag.Hash);
        var second = RecordingWriter.Mask(ValueMaskStrategy.Default, "x", ValueKind.String, MaskTag.Hash);

        first.Should().Be(second).And.NotBe(RecordingWriter.Mask(ValueMaskStrategy.Default, "y", ValueKind.String, MaskTag.Hash));
    }

    [Fact]
    public void Hash_OfContainersAndNull_IsStars()
    {
        RecordingWriter.Mask(ValueMaskStrategy.Default, "", ValueKind.Object, MaskTag.Hash).Should().Be("\"***\"");
        RecordingWriter.Mask(ValueMaskStrategy.Default, "", ValueKind.Null, MaskTag.Hash).Should().Be("\"***\"");
    }

    [Theory]
    [InlineData(10)]
    [InlineData(1000)]
    public void Hash_AllocatesNothingWhenWarm(int length)
    {
        var strategy = ValueMaskStrategy.Default;
        var options = ObserverOptions.Default.WithBase64HashKey(Base64Key) with { HashKeyId = 3 };
        var value = Encoding.UTF8.GetBytes(new string('k', length));
        var writer = new CountingWriter();
        var path = new DataPath(default, 4, true);
        try
        {
            path.PushName("email"u8);
            for (var i = 0; i < 3; i++)
            {
                strategy.Mask(new MaskContext(value, ValueKind.String, MaskTag.Hash, options, in path, i), writer);
            }

            var before = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 100; i++)
            {
                strategy.Mask(new MaskContext(value, ValueKind.String, MaskTag.Hash, options, in path, i), writer);
            }

            (GC.GetAllocatedBytesForCurrentThread() - before).Should().Be(0);
            writer.Length.Should().Be(2 + 24);
        }
        finally
        {
            path.Dispose();
        }
    }

    private static HmacRedactor Redactor(int? keyId)
#pragma warning disable EXTEXP0002
        => new(Options.Create(new HmacRedactorOptions { Key = Base64Key, KeyId = keyId }));
#pragma warning restore EXTEXP0002

    private sealed class CountingWriter : MaskValueWriter
    {
        public int Length { get; private set; }

        public override void String(ReadOnlySpan<byte> utf8) => Length = utf8.Length;

        public override void String(ReadOnlySpan<char> chars) => Length = chars.Length;

        public override void Boolean(bool value) => Length = 0;

        public override void Null() => Length = 0;

        public override void Keep() => Length = 0;

        protected override void WriteNumber(ReadOnlySpan<byte> utf8Literal) => Length = utf8Literal.Length;
    }
}

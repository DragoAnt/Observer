# Hashing

`MaskKind.Hash` replaces a value by a keyed hash, so that equal values correlate across log lines without being shown. The built-in `ValueMaskStrategy` writes **the same text as Microsoft's [`HmacRedactor`](https://learn.microsoft.com/dotnet/api/microsoft.extensions.compliance.redaction.hmacredactor)** (package [Microsoft.Extensions.Compliance.Redaction](https://www.nuget.org/packages/Microsoft.Extensions.Compliance.Redaction)) for the same key and key id, so a value hashed by an observer matches the same value redacted by Microsoft's redactor elsewhere.

| Step | What |
| --- | --- |
| Input | a string's unescaped text; a number's or boolean's literal (`12.50`, `true`) |
| Bytes hashed | the input's UTF-16 code units (little-endian) — the value kind is not hashed, so `1` and `"1"` hash alike |
| Hash | HMAC-SHA256 with `ObserverOptions.HashKey`; an empty key means a random key for the lifetime of the process |
| Output | the first 16 bytes in base64: 24 characters, ending in `==` |
| Key id | `"<HashKeyId>:"` before the hash when `HashKeyId` is set, so `7:` and the 24 characters |
| Empty | `""` hashes to `""` |
| Objects, arrays, `null` | written as `"***"` — a container is never hashed |

`WithBase64HashKey(key)` takes the key the way Microsoft's `HmacRedactorOptions.Key` holds it — a base64 string of at least 44 characters — and keeps the options type:

```csharp
using System.Text;
using DragoAnt.Observer;

var options = new ObserverOptions { HashKeyId = 7 }
    .WithBase64HashKey("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+Pw==");

var output = new TextOutput();
var path = new DataPath(default, 1, nameCaseInsensitive: true);
path.PushName("email"u8);
ValueMaskStrategy.Default.Mask(new MaskContext("a@b.c"u8, ValueKind.String, MaskTag.Hash, options, in path, 0), output);
path.Dispose();

Console.WriteLine(output.Text.StartsWith("7:") && output.Text.Length == 2 + 24);
// True

sealed class TextOutput : MaskValueWriter
{
    public string Text { get; private set; } = "";

    public override void String(ReadOnlySpan<byte> utf8) => Text = Encoding.UTF8.GetString(utf8);

    public override void String(ReadOnlySpan<char> chars) => Text = new string(chars);

    public override void Boolean(bool value) => Text = value ? "true" : "false";

    public override void Null() => Text = "null";

    public override void Keep() => Text = "(kept)";

    protected override void WriteNumber(ReadOnlySpan<byte> utf8Literal) => Text = Encoding.UTF8.GetString(utf8Literal);
}
```

A warm hash allocates nothing: the value is transcoded into a stack buffer (or a pooled one above 256 characters), hashed with the static `HMACSHA256.HashData`, and encoded straight into the output. The test suite compares a corpus — ASCII, non-ASCII, surrogate pairs, numbers, booleans, long values — with `HmacRedactor` from Microsoft.Extensions.Compliance.Redaction 10.10.0, with and without a key id.

The hash format before 2.0 of DragoAnt.System.Text.Json.Observer (`hash:` and 16 hex characters over UTF-8) is gone; stored hashes of that format do not match.

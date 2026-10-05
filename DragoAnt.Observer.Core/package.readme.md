# DragoAnt.Observer.Core

The format-neutral core of the DragoAnt observers. A format package — [DragoAnt.System.Text.Json.Observer](https://www.nuget.org/packages/DragoAnt.System.Text.Json.Observer) for JSON — walks its input and uses these types, so a strategy, a policy or a result means the same thing for every format:

- `ObserverOptions` — limits, the hash key and key id, the strategy, the case option, `IgnoreNulls`, the comment policy.
- `ValueMaskStrategy` with `MaskContext` and `MaskValueWriter` — masks one value; the built-in strategy's `Hash` writes exactly what Microsoft's `HmacRedactor` writes for the same key.
- `ValuePolicy` (`AllowList`, `BlockList`, `NullList`, `Tagged(tag)`) and `CommentPolicy` with comment rules.
- `MaskResult`, `MaskStatus`, `MaskFlags`, `PathExplanation` — what a call did and why.
- `DataPath`, `NameMatch` and `Names` — where a value is and how rules test names.

```csharp
using DragoAnt.Observer;

var options = new ObserverOptions { HashKeyId = 7, MaxValueBytes = 256 }
    .WithBase64HashKey("AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8gISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0+Pw==");
Console.WriteLine(options.HashKey.Length);
// 64
```

A custom strategy serves every format:

```csharp
using DragoAnt.Observer;

sealed class Redacted : ValueMaskStrategy
{
    public override void Mask(in MaskContext context, MaskValueWriter output)
    {
        if (context.Tag.Kind == MaskKind.Full)
        {
            output.String("[redacted]"u8);
            return;
        }

        Default.Mask(context, output);
    }
}
```

- [Repository](https://github.com/DragoAnt/Observer) · [Comments](https://github.com/DragoAnt/Observer/blob/main/docs/comments.md) · [Hashing](https://github.com/DragoAnt/Observer/blob/main/docs/hashing.md) · [Issues](https://github.com/DragoAnt/Observer/issues)

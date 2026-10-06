# DragoAnt.Observer

Format-neutral building blocks of the DragoAnt observers — mask tags, paths, name tests, results, options, value and comment policies, and the masking strategy — so that one strategy or policy works for JSON today and for CSV and YAML next.

<!-- nuget:skip -->
[![CI](https://img.shields.io/github/actions/workflow/status/DragoAnt/Observer/ci.yml?branch=main)](https://github.com/DragoAnt/Observer/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/DragoAnt.Observer.Core)](https://www.nuget.org/packages/DragoAnt.Observer.Core)
[![License](https://img.shields.io/github/license/DragoAnt/Observer)](https://github.com/DragoAnt/Observer/blob/main/LICENSE)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0%20%7C%20netstandard2.0-512BD4)
<!-- /nuget:skip -->

## Packages

| Package | Targets | What |
| --- | --- | --- |
| [DragoAnt.Observer.Abstractions](https://www.nuget.org/packages/DragoAnt.Observer.Abstractions) | `netstandard2.0` | `MaskKind`, `MaskTag` — no dependencies, for model libraries |
| [DragoAnt.Observer.Core](https://www.nuget.org/packages/DragoAnt.Observer.Core) | `net8.0`, `net10.0` | options, strategy, policies, results, paths, name tests |

You usually reference a format package instead, which brings Core: [DragoAnt.System.Text.Json.Observer](https://www.nuget.org/packages/DragoAnt.System.Text.Json.Observer) for JSON.

<!-- nuget:only DragoAnt.Observer.Core -->
## Key features

- **One strategy for every format** — `ValueMaskStrategy.Mask(in MaskContext, MaskValueWriter)` sees the whole value, its kind, the tag, the path and its position, and writes through a format's writer, which validates numbers.
- **The hash of Microsoft's `HmacRedactor`** — `MaskKind.Hash` writes the same text for the same key and key id, so hashes correlate with logs redacted by Microsoft.Extensions.Compliance.Redaction ([hashing](./docs/hashing.md)).
- **Policies as values** — `ValuePolicy.AllowList` / `BlockList` / `NullList` / `Tagged(tag)` for unmatched values, and `CommentPolicy` with comment rules for comments ([comments](./docs/comments.md)).
- **Results that say why** — `MaskResult` with `MaskStatus` and `MaskFlags` (`InputTruncated`, `OutputCapped`, `ValueCut`, `TrailingData`, `InvalidUtf8Replaced`, `Depth`).
- **Allocation-free paths and name tests** — `DataPath` keeps names as UTF-8 and formats into a span; `Names` tests compare UTF-8 bytes and follow one case option.
<!-- /nuget:only -->

## Install

<!-- nuget:only DragoAnt.Observer.Core -->
```sh
dotnet add package DragoAnt.Observer.Core
```
<!-- /nuget:only -->

<!-- nuget:only DragoAnt.Observer.Abstractions -->
A model library that only names how its values are masked needs just the vocabulary:

```sh
dotnet add package DragoAnt.Observer.Abstractions
```
<!-- /nuget:only -->

<!-- nuget:only DragoAnt.Observer.Core -->
## Quick start

```csharp
using DragoAnt.Observer;

var options = new ObserverOptions { MaxValueBytes = 256, HashKeyId = 7 };
Console.WriteLine(ValuePolicy.Tagged(MaskTag.Hash));
Console.WriteLine(MaskTag.Custom("email"));
Console.WriteLine(options.Comments);
// Tagged(Hash)
// Custom (email)
// AllowList
```

The hash key is set from base64; the built-in strategy's `Hash` then writes exactly what Microsoft's `HmacRedactor` writes for the same key:

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
<!-- /nuget:only -->

<!-- nuget:only DragoAnt.Observer.Abstractions -->
## Abstractions — the masking vocabulary

`DragoAnt.Observer.Abstractions` has no dependencies: `MaskKind` (`Full`, `Last4`, `Hash`, `Null`, `Custom`) and `MaskTag`, a kind plus an optional classification key that a custom strategy maps to its own masking. It targets `netstandard2.0`, so a model library can name how its values are masked without referencing an observer.

```csharp
using DragoAnt.Observer;

var tag = MaskTag.Custom("email");
Console.WriteLine(tag);
Console.WriteLine(MaskTag.Hash.WithKey("pii"));
// Custom (email)
// Hash (pii)
```

From version 1.1 this package will warn when a project builds with a .NET SDK older than 10 (the analyzers that ship then need it), and from 1.2 it will be an error; `<DragoAntObserverAllowOldCompiler>true</DragoAntObserverAllowOldCompiler>` keeps it a warning. Apps may still target `net8.0`.
<!-- /nuget:only -->

<!-- nuget:skip -->
## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md); report vulnerabilities as described in [SECURITY.md](./SECURITY.md). Licensed under the [MIT licence](./LICENSE).
<!-- /nuget:skip -->

---

*Hand-written, extended with AI.*

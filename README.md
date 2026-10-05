# DragoAnt.Observer

Format-neutral building blocks of the DragoAnt observers — mask tags, paths, name tests, results, options, value and comment policies, and the masking strategy — so that one strategy or policy works for JSON today and for CSV and YAML next.

[![CI](https://img.shields.io/github/actions/workflow/status/DragoAnt/Observer/ci.yml?branch=main)](https://github.com/DragoAnt/Observer/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/DragoAnt.Observer.Core)](https://www.nuget.org/packages/DragoAnt.Observer.Core)
[![License](https://img.shields.io/github/license/DragoAnt/Observer)](https://github.com/DragoAnt/Observer/blob/main/LICENSE)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%2010.0%20%7C%20netstandard2.0-512BD4)

## Packages

| Package | Targets | What |
| --- | --- | --- |
| [DragoAnt.Observer.Abstractions](https://www.nuget.org/packages/DragoAnt.Observer.Abstractions) | `netstandard2.0` | `MaskKind`, `MaskTag` — no dependencies, for model libraries |
| [DragoAnt.Observer.Core](https://www.nuget.org/packages/DragoAnt.Observer.Core) | `net8.0`, `net10.0` | options, strategy, policies, results, paths, name tests |

You usually reference a format package instead, which brings Core: [DragoAnt.System.Text.Json.Observer](https://www.nuget.org/packages/DragoAnt.System.Text.Json.Observer) for JSON.

## Key features

- **One strategy for every format** — `ValueMaskStrategy.Mask(in MaskContext, MaskValueWriter)` sees the whole value, its kind, the tag, the path and its position, and writes through a format's writer, which validates numbers.
- **The hash of Microsoft's `HmacRedactor`** — `MaskKind.Hash` writes the same text for the same key and key id, so hashes correlate with logs redacted by Microsoft.Extensions.Compliance.Redaction ([hashing](./docs/hashing.md)).
- **Policies as values** — `ValuePolicy.AllowList` / `BlockList` / `NullList` / `Tagged(tag)` for unmatched values, and `CommentPolicy` with comment rules for comments ([comments](./docs/comments.md)).
- **Results that say why** — `MaskResult` with `MaskStatus` and `MaskFlags` (`InputTruncated`, `OutputCapped`, `ValueCut`, `TrailingData`, `InvalidUtf8Replaced`, `Depth`).
- **Allocation-free paths and name tests** — `DataPath` keeps names as UTF-8 and formats into a span; `Names` tests compare UTF-8 bytes and follow one case option.

## Install

```sh
dotnet add package DragoAnt.Observer.Core
```

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

## Contributing

See [CONTRIBUTING.md](./CONTRIBUTING.md); report vulnerabilities as described in [SECURITY.md](./SECURITY.md). Licensed under the [MIT licence](./LICENSE).

---

*Hand-written, extended with AI.*

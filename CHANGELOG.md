# Changelog

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - Unreleased

First release of [DragoAnt.Observer.Abstractions](https://www.nuget.org/packages/DragoAnt.Observer.Abstractions) and [DragoAnt.Observer.Core](https://www.nuget.org/packages/DragoAnt.Observer.Core). The types come from [DragoAnt.System.Text.Json.Observer](https://www.nuget.org/packages/DragoAnt.System.Text.Json.Observer) 1.x under their final, format-neutral names; its 2.0 builds on them ([migrating to 2.0](https://github.com/DragoAnt/Extensions.System.Text.Json/blob/main/docs/migrating-to-2.0.md)).

**Build requirement (coming in 1.1 as a warning, 1.2 as an error):** the .NET 10 SDK. 1.0 builds with any SDK that supports its targets; apps may keep targeting `net8.0`, and `<DragoAntObserverAllowOldCompiler>true</DragoAntObserverAllowOldCompiler>` will keep the check a warning.

### Added

- **Abstractions** (`netstandard2.0`): `MaskKind` (`Full`, `Last4`, `Hash`, `Null`, `Custom`) and `MaskTag` with `Full`, `Last4`, `Hash`, `Null`, `Custom(key)`, `Create(kind, key)`, `WithKey`, `TryGetKey<T>`.
- **Options:** `ObserverOptions` — `MaxOutputBytes`, `MaxValueBytes`, `MaxDepth`, `HashKey`, `HashKeyId`, `Strategy`, `NameCaseInsensitive`, `IgnoreNulls`, `Comments` — and `WithBase64HashKey(key)`, which keeps a derived options type.
- **Strategy:** `ValueMaskStrategy` with `Default`, `MaskContext` (`Value`, `Kind`, `Tag`, `Options`, `Path`, `Name`, `IsArrayItem`, `ValueIndex`) and `MaskValueWriter`, whose `Number` validates the literal.
- **Hash:** `MaskKind.Hash` writes the output of Microsoft's `HmacRedactor`: HMAC-SHA256 over UTF-16, 16 bytes in base64, `"<HashKeyId>:"` first when set, `""` for `""`; 0 B per warm call.
- **Policies:** `ValuePolicy` (`BlockList`, `AllowList`, `NullList`, `Tagged(tag)`), `MaskNulls`, and the comment model — `CommentPolicy` (`AllowList` default, `BlockList`, `MaskAll`, `Mask(tag)`, `DropAll`), `CommentKind`, `CommentStyle`, `CommentContext`, `CommentRule`, `CommentRules`.
- **Results:** `MaskResult` (`Status`, `BytesWritten`, `FailedAtByte`, `Flags`), `MaskStatus` (`Masked`, `Truncated`, `Unrecognized`, `Invalid`), `MaskFlags`, `PathExplanation`, `PathOutcome`.
- **Paths and names:** `DataPath` (UTF-8 names, item positions, `ToString`, `TryFormat`), `NameMatch` and `Names` (`StartsWith`, `EndsWith`, `Contains`, `OneOf`, `Regex`), `UnknownMemberPolicy`, `ValueKind`, `NoContext`.

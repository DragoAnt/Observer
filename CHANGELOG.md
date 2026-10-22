# Changelog

All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - Unreleased

First release of [DragoAnt.Observer.Abstractions](https://www.nuget.org/packages/DragoAnt.Observer.Abstractions) and [DragoAnt.Observer.Core](https://www.nuget.org/packages/DragoAnt.Observer.Core). The types come from [DragoAnt.System.Text.Json.Observer](https://www.nuget.org/packages/DragoAnt.System.Text.Json.Observer) 1.x under their final, format-neutral names; its 2.0 builds on them ([migrating to 2.0](https://github.com/DragoAnt/Extensions.System.Text.Json/blob/main/docs/migrating-to-2.0.md)).

**Build requirement (coming in 1.1 as a warning, 1.2 as an error):** the .NET 10 SDK. 1.0 builds with any SDK that supports its targets; apps may keep targeting `net8.0`, and `<DragoAntObserverAllowOldCompiler>true</DragoAntObserverAllowOldCompiler>` will keep the check a warning.

### Added

- **Format-neutral rules (experimental, `DAOEXP001`):** `MaskRules.Create(b => b.Match(name)… .Path(levels…)… , unmatched)` with `Mask(tag)`, `Unmasked()` and `Comment(kinds, rule)`; a Path rule wins over a Match rule; format implementations read the rules through `MaskRules.Rules` ([experimental API](./docs/experimental.md)).
- **Format seam (experimental, `DAOEXP002`):** `IObserverFormat` (`Name`, `MediaTypes`, `Bind(MaskRules)`) and `IObserverFormatBinding` with a synchronous `Mask(span, writer)` for one document in memory and `MaskAsync(PipeReader, PipeWriter)` for a run of records; `System.IO.Pipelines` 8.0.0 on `net8.0`.
- **Conformance kit (experimental, `DAOEXP002`):** `DragoAnt.Observer.Conformance` — scenarios with one `MaskRules` declaration and a document per format, and the invariants (leak on every prefix, status, visible values, tag output, round trip, sync and async alike) that `ConformanceKit.RunAsync(format, traits)` checks for any `IObserverFormat`.
- **Abstractions** (`netstandard2.0`): `MaskKind` (`Full`, `Last4`, `Hash`, `Null`, `Custom`) and `MaskTag` with `Full`, `Last4`, `Hash`, `Null`, `Custom(key)`, `Create(kind, key)`, `WithKey`, `TryGetKey<T>`.
- **Options:** `ObserverOptions` — `MaxOutputBytes`, `MaxValueBytes`, `MaxDepth`, `MaxRecordBytes`, `MaxMaskValueBytes`, `HashKey`, `HashKeyId`, `Strategy`, `NameCaseInsensitive`, `IgnoreNulls`, `Comments` — every limit per document, a record of a multi-record format being one; and `WithBase64HashKey(key)`, which keeps a derived options type.
- **Strategy:** `ValueMaskStrategy` with `Default`, `MaskContext` (`Value`, `Kind`, `Tag`, `Options`, `Path`, `Name`, `IsArrayItem`, `ValueIndex`) and `MaskValueWriter`, whose `Number` validates the literal. `MaskContext.Value` is the whole value up to `MaxMaskValueBytes`; a longer one is written as `***` without the strategy (`MaskFlags.ValueTooLarge`).
- **Hash:** `MaskKind.Hash` writes the output of Microsoft's `HmacRedactor`: HMAC-SHA256 over UTF-16, 16 bytes in base64, `"<HashKeyId>:"` first when set, `""` for `""`; 0 B per warm call. A `ValueKind.Binary` value hashes its raw bytes.
- **Policies:** `ValuePolicy` (`BlockList`, `AllowList`, `NullList`, `Tagged(tag)`), `MaskNulls`, and the comment model — `CommentPolicy` (`AllowList` default, `BlockList`, `MaskAll`, `Mask(tag)`, `DropAll`), `CommentKind`, `CommentStyle` (`Line`, `Block`, `Hash`, `Semicolon`, `Exclamation`, `Markup`), `CommentContext`, `CommentRule`, `CommentRules`. In a format without closing brackets a comment belongs to the next member.
- **Results:** `MaskResult` for one document (`Status`, `BytesWritten` as `long`, `FailedAtByte`, `Flags`), `MaskRecordsResult` for a run of records (worst `Status`, `Flags`, `Records`, `FailedRecords`, `DroppedRecords`, `FirstFailedRecord`, `BytesRead`, `BytesWritten`, `FromDocument`), `MaskStatus` (`Masked`, `Truncated`, `Unrecognized`, `Invalid`, defined the same for every format — [results](./docs/results.md)), `MaskFlags` (with `ValueTooLarge`, `RecordFailed`), `PathExplanation`, `PathOutcome`.
- **Paths and names:** `DataPath` (UTF-8 names, item positions, `RecordIndex`, `ToString`, `TryFormat`), `NameMatch` and `Names` (`StartsWith`, `EndsWith`, `Contains`, `OneOf`, `Regex`), `UnknownMemberPolicy`, `ValueKind` (with `Binary` for opaque payloads; `Null` is the format's no-value form), `NoContext`.

### Changed since 1.0.0-beta.1

- `MaskResult.BytesWritten` is a `long`.
- Limits are documented per document; `MaskStatus` and the no-value members (`ValueKind.Null`, `MaskValueWriter.Null`, `MaskKind.Null`, `ValuePolicy.NullList`, `IgnoreNulls`) are worded for every format, with no change for JSON.

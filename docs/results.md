# Results

Every observer call returns a `MaskResult` for one document; a format that reads many records from one input (NDJSON, logfmt) returns a `MaskRecordsResult` for the run, and a failed record never ends it.

| Status | Meaning in every format |
| --- | --- |
| `Masked` | everything was read and written in full; no flags |
| `Truncated` | the output is valid, the flags say what did not come through: `InputTruncated`, `OutputCapped`, `ValueCut`, `ValueTooLarge`, `TrailingData`, `InvalidUtf8Replaced` |
| `Unrecognized` | nothing could be read as this format — empty where a document is required, or the first token cannot start one; nothing written |
| `Invalid` | the input breaks the grammar or the expected shape, or a rule failed; the masked part before the failure is written, closed |

## Per format

| Format | `Unrecognized` | `Invalid` |
| --- | --- | --- |
| JSON | empty input; a root that is a string, number, `true`, `false` or `null` | broken JSON; a root array given to `JsonObserver.Obj`, a root object to `JsonObserver.Array`; a strategy that wrote an invalid number; deeper than `MaxDepth` (with `Depth`) |

Each format package adds its row when it ships.

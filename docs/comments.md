# Comments

Formats with comments (JSONC today, YAML and CSV comment rows later) handle them with the same model as values: a global **comment policy**, overridden by a **comment rule** on the path rule that owns the comment. Every comment is **dropped by default**, so a comment never reaches a log unless something asks for it.

## The policy

`ObserverOptions.Comments` (a [`CommentPolicy`](../DragoAnt.Observer.Core/Comments.cs)) decides what happens to a comment no rule decides:

| Policy | A comment no rule decides |
| --- | --- |
| `CommentPolicy.AllowList` (default) | dropped |
| `CommentPolicy.BlockList` | kept; masked when its owner's value is masked |
| `CommentPolicy.MaskAll` | kept, its text replaced by `***` |
| `CommentPolicy.Mask(tag)` | kept, its text masked by the call's strategy with `tag` |
| `CommentPolicy.DropAll` | dropped, and comment rules are ignored — for output that must never carry a comment, such as a log line |

## Comment rules

A rule decides the comments of the members it matches, for the placements it names:

```csharp
var observer = JsonObserver.Obj(AnyDepth(rules => rules
        .Match("password").Mask(MaskTag.Full).Comment(CommentKind.Any, CommentRules.Drop)
        .Match("timeout").Unmasked().Comment(CommentKind.Inline, (ref CommentContext c) =>
        {
            if (c.Text.TrimStart(" "u8).StartsWith("TODO"u8)) c.Drop(); else c.Keep();
        }),
    BlockList));
```

`CommentRules` has `Keep`, `Raw`, `Drop` and `Mask(tag)`; a custom rule gets a `CommentContext` with the text (UTF-8, without markers), the `Kind`, the `Style`, the `Owner` path and `OwnerMasked`, and calls `Keep`, `Raw`, `Drop`, `Mask(tag)` or `Replace(text)`.

## Who owns a comment

| Placement | Comments | Owner |
| --- | --- | --- |
| `CommentKind.Before` | every comment between the previous member's line and a member — the space between its name and value included | the member |
| `CommentKind.Inline` | on the same line as a member's value, after it | the member |
| `CommentKind.After` | after the last member of an object or array, before it closes, or after the document | the container |

A comment has exactly one owner. **A kept comment of a masked value is written masked** (`Keep` under a masked owner becomes `Mask(MaskTag.Full)`); only `CommentRules.Raw` writes it in clear — the comment twin of an unmasked value.

## In JSON

| Input | Default | `CommentPolicy.BlockList` | `BlockList` with the rules above |
| --- | --- | --- | --- |
| `{ // service account`<br>`"user": "svc-orders",`<br>`"password": "hunter2", // rotated 2026-09`<br>`/* timeouts in seconds */`<br>`"timeout": 30 // TODO: lower }` | `{"user":"svc-orders","password":"***","timeout":30}` | `{/* service account*/"user":"svc-orders","password":"***"/*****//* timeouts in seconds */,"timeout":30/* TODO: lower*/}` | `{/* service account*/"user":"svc-orders","password":"***"/* timeouts in seconds */,"timeout":30}` |

- Kept comments are written as `/* … */`, before the comma that follows them; read the output with `JsonCommentHandling.Allow` or `Skip`. A `*/` inside a comment's text is written as `* /`.
- `MaxOutputBytes` counts comment bytes; a cut output still parses.
- When the policy and the rules would drop every comment, the reader skips them and the call costs exactly what it did without comments.
- A line comment at the very end of the input, with no line break after it, is not read: the reader cannot tell it is complete.
- Reading (`Read`) never writes comments. The HTTP body logging of `DragoAnt.System.Text.Json.Observer.Http` uses `DropAll`.

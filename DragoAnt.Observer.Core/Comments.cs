using System.ComponentModel;

namespace DragoAnt.Observer;

/// <summary>
/// Where a comment stands relative to the member or item that owns it.
/// </summary>
[Flags]
public enum CommentKind
{
    /// <summary>
    /// Before the member: every comment between the previous member's line and the member, the member's name included.
    /// </summary>
    Before = 1,

    /// <summary>
    /// After the last member of an object or array, or after the document: owned by the container.
    /// </summary>
    After = 2,

    /// <summary>
    /// On the same line as the member's value, after it.
    /// </summary>
    Inline = 4,

    /// <summary>
    /// Every placement.
    /// </summary>
    Any = Before | After | Inline,
}

/// <summary>
/// How a comment is written in the input.
/// </summary>
public enum CommentStyle
{
    /// <summary>
    /// A line comment, <c>// …</c>.
    /// </summary>
    Line,

    /// <summary>
    /// A block comment, <c>/* … */</c>.
    /// </summary>
    Block,

    /// <summary>
    /// A hash comment, <c># …</c>, as in YAML.
    /// </summary>
    Hash,
}

/// <summary>
/// The built-in comment policies a <see cref="CommentPolicy"/> stands for.
/// </summary>
public enum CommentPolicyKind
{
    /// <summary>
    /// A comment no rule keeps is dropped.
    /// </summary>
    AllowList,

    /// <summary>
    /// A comment no rule drops is kept.
    /// </summary>
    BlockList,

    /// <summary>
    /// Every comment is kept with its text masked by <see cref="CommentPolicy.Tag"/>.
    /// </summary>
    Mask,

    /// <summary>
    /// Every comment is dropped, whatever a rule says.
    /// </summary>
    DropAll,
}

/// <summary>
/// What happens to comments no rule decides — the same model as <see cref="ValuePolicy"/> for values. A comment rule
/// on a path rule overrides it for the comments that rule's members own.
/// </summary>
public sealed class CommentPolicy
{
    private CommentPolicy(CommentPolicyKind kind, MaskTag tag)
    {
        Kind = kind;
        Tag = tag;
    }

    /// <summary>
    /// The default: a comment no rule keeps is dropped, so a comment never reaches the output unless asked for.
    /// </summary>
    public static CommentPolicy AllowList { get; } = new(CommentPolicyKind.AllowList, MaskTag.Full);

    /// <summary>
    /// A comment no rule drops is kept; one that a masked member owns is written masked.
    /// </summary>
    public static CommentPolicy BlockList { get; } = new(CommentPolicyKind.BlockList, MaskTag.Full);

    /// <summary>
    /// Every comment is kept with its text replaced by <c>***</c>.
    /// </summary>
    public static CommentPolicy MaskAll { get; } = new(CommentPolicyKind.Mask, MaskTag.Full);

    /// <summary>
    /// Every comment is dropped and comment rules are ignored, for output such as log lines that must never carry one.
    /// </summary>
    public static CommentPolicy DropAll { get; } = new(CommentPolicyKind.DropAll, MaskTag.Full);

    /// <summary>
    /// Every comment is kept with its text masked by the call's strategy and <paramref name="tag"/>.
    /// </summary>
    /// <param name="tag">How the text is masked.</param>
    public static CommentPolicy Mask(MaskTag tag) => new(CommentPolicyKind.Mask, tag);

    /// <summary>
    /// Which built-in policy this is.
    /// </summary>
    public CommentPolicyKind Kind { get; }

    /// <summary>
    /// The tag of a <see cref="CommentPolicyKind.Mask"/> policy.
    /// </summary>
    public MaskTag Tag { get; }

    /// <summary>
    /// Decides one comment: the policy's answer, then <paramref name="rule"/> when one applies, then the masked-owner
    /// rule (a kept comment of a masked member is written masked). For format implementations.
    /// </summary>
    /// <param name="context">The comment; receives the decision.</param>
    /// <param name="rule">The comment rule of the owner's path rule for this placement, or <c>null</c>.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void Decide(ref CommentContext context, CommentRule? rule)
    {
        switch (Kind)
        {
            case CommentPolicyKind.DropAll:
                context.Drop();
                return;
            case CommentPolicyKind.BlockList:
                context.Keep();
                break;
            case CommentPolicyKind.Mask:
                context.Mask(Tag);
                break;
            default:
                context.Drop();
                break;
        }

        rule?.Invoke(ref context);
        if (context.Action == CommentAction.Keep && context.OwnerMasked)
        {
            context.Mask(MaskTag.Full);
        }
    }

    /// <summary>
    /// The policy's name, for example <c>AllowList</c> or <c>Mask(Hash)</c>.
    /// </summary>
    public override string ToString() => Kind == CommentPolicyKind.Mask && Tag != MaskTag.Full ? $"Mask({Tag})" : Kind == CommentPolicyKind.Mask ? "MaskAll" : Kind.ToString();
}

/// <summary>
/// What a comment rule decided for one comment.
/// </summary>
public enum CommentAction
{
    /// <summary>
    /// Not written.
    /// </summary>
    Drop,

    /// <summary>
    /// Written as it is, or masked when its owner's value is masked.
    /// </summary>
    Keep,

    /// <summary>
    /// Written as it is even when its owner's value is masked.
    /// </summary>
    Raw,

    /// <summary>
    /// Written with its text masked by <see cref="CommentContext.MaskTag"/>.
    /// </summary>
    Mask,

    /// <summary>
    /// Written with <see cref="CommentContext.Replacement"/> as its text.
    /// </summary>
    Replace,
}

/// <summary>
/// One comment a rule decides about. Valid only during the call it is passed to.
/// </summary>
public ref struct CommentContext
{
    private readonly DataPath _owner;

    /// <summary>
    /// Creates the context of one comment, dropped until decided. For format implementations.
    /// </summary>
    /// <param name="text">UTF-8 text without the comment markers.</param>
    /// <param name="kind">Placement relative to the owner.</param>
    /// <param name="style">How the comment is written.</param>
    /// <param name="owner">Path of the member, item or container that owns the comment.</param>
    /// <param name="ownerMasked">The owner's value is masked.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public CommentContext(ReadOnlySpan<byte> text, CommentKind kind, CommentStyle style, in DataPath owner, bool ownerMasked)
    {
        Text = text;
        Kind = kind;
        Style = style;
        _owner = owner;
        OwnerMasked = ownerMasked;
        Action = CommentAction.Drop;
    }

    /// <summary>
    /// The comment's UTF-8 text, without the comment markers.
    /// </summary>
    public ReadOnlySpan<byte> Text { get; }

    /// <summary>
    /// Placement relative to the owner.
    /// </summary>
    public CommentKind Kind { get; }

    /// <summary>
    /// How the comment is written in the input.
    /// </summary>
    public CommentStyle Style { get; }

    /// <summary>
    /// Path of the member or item the comment belongs to; for <see cref="CommentKind.After"/>, of the container.
    /// </summary>
    public readonly DataPath Owner => _owner;

    /// <summary>
    /// The owner's value is masked, so <see cref="Keep"/> writes the comment masked.
    /// </summary>
    public bool OwnerMasked { get; }

    /// <summary>
    /// The decision so far.
    /// </summary>
    public CommentAction Action { readonly get; private set; }

    /// <summary>
    /// The tag of a <see cref="CommentAction.Mask"/> decision.
    /// </summary>
    public MaskTag MaskTag { readonly get; private set; }

    /// <summary>
    /// The text of a <see cref="CommentAction.Replace"/> decision.
    /// </summary>
    public string? Replacement { readonly get; private set; }

    /// <summary>
    /// Keeps the comment; it is written masked when <see cref="OwnerMasked"/> is set.
    /// </summary>
    public void Keep() => Action = CommentAction.Keep;

    /// <summary>
    /// Keeps the comment in clear text even when its owner is masked — the comment twin of an unmasked value.
    /// </summary>
    public void Raw() => Action = CommentAction.Raw;

    /// <summary>
    /// Drops the comment.
    /// </summary>
    public void Drop() => Action = CommentAction.Drop;

    /// <summary>
    /// Keeps the comment with its text masked by the call's strategy and <paramref name="tag"/>.
    /// </summary>
    /// <param name="tag">How the text is masked.</param>
    public void Mask(MaskTag tag)
    {
        Action = CommentAction.Mask;
        MaskTag = tag;
    }

    /// <summary>
    /// Keeps the comment with another text.
    /// </summary>
    /// <param name="text">The new text, without comment markers.</param>
    public void Replace(ReadOnlySpan<char> text)
    {
        Action = CommentAction.Replace;
        Replacement = text.ToString();
    }
}

/// <summary>
/// Decides one comment, for example <see cref="CommentRules.Drop"/>.
/// </summary>
/// <param name="context">The comment and the decision so far.</param>
public delegate void CommentRule(ref CommentContext context);

/// <summary>
/// Ready-made comment rules.
/// </summary>
public static class CommentRules
{
    /// <summary>
    /// Keeps the comment, masked when its owner is masked.
    /// </summary>
    public static CommentRule Keep { get; } = static (ref CommentContext c) => c.Keep();

    /// <summary>
    /// Keeps the comment in clear text even when its owner is masked.
    /// </summary>
    public static CommentRule Raw { get; } = static (ref CommentContext c) => c.Raw();

    /// <summary>
    /// Drops the comment.
    /// </summary>
    public static CommentRule Drop { get; } = static (ref CommentContext c) => c.Drop();

    /// <summary>
    /// Keeps the comment with its text masked by <paramref name="tag"/>.
    /// </summary>
    /// <param name="tag">How the text is masked.</param>
    public static CommentRule Mask(MaskTag tag) => (ref CommentContext c) => c.Mask(tag);
}

using System.Diagnostics.CodeAnalysis;

namespace DragoAnt.Observer.Formats;

/// <summary>One rule of a <see cref="MaskRules"/>, as a format implementation reads it.</summary>
[Experimental(ExperimentalIds.MaskRules, UrlFormat = ExperimentalIds.Url)]
public sealed class MaskRule
{
    internal MaskRule(MaskRuleScope scope, NameMatch[] levels, MaskRuleAction action, MaskTag tag, CommentKind commentKinds, CommentRule? commentRule)
        => (Scope, Levels, Action, Tag, CommentKinds, CommentRule) = (scope, Array.AsReadOnly(levels), action, tag, commentKinds, commentRule);

    /// <summary>Where the rule looks.</summary>
    public MaskRuleScope Scope { get; }

    /// <summary>The rule's name tests.</summary>
    public IReadOnlyList<NameMatch> Levels { get; }

    /// <summary>What happens to a matched value.</summary>
    public MaskRuleAction Action { get; }

    /// <summary>The rule's masking tag.</summary>
    public MaskTag Tag { get; }

    /// <summary>The placements decided by <see cref="CommentRule"/>.</summary>
    public CommentKind CommentKinds { get; }

    /// <summary>The rule's comment rule, or null.</summary>
    public CommentRule? CommentRule { get; }

    /// <summary>The rule as declared.</summary>
    public override string ToString() => $"{(Scope == MaskRuleScope.AnyDepth ? "Match" : "Path")}({string.Join(", ", Levels)}) → {(Action == MaskRuleAction.Mask ? $"Mask({Tag})" : "Unmasked")}";
}

/// <summary>Where a <see cref="MaskRule"/> looks.</summary>
[Experimental(ExperimentalIds.MaskRules, UrlFormat = ExperimentalIds.Url)]
public enum MaskRuleScope
{
    /// <summary>At any depth.</summary>
    AnyDepth,

    /// <summary>From the root.</summary>
    FromRoot,
}

/// <summary>What a <see cref="MaskRule"/> does.</summary>
[Experimental(ExperimentalIds.MaskRules, UrlFormat = ExperimentalIds.Url)]
public enum MaskRuleAction
{
    /// <summary>Masks the value.</summary>
    Mask,

    /// <summary>Writes it unchanged.</summary>
    Unmasked,
}

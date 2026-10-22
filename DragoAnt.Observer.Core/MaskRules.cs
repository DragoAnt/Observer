using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using DragoAnt.Observer.Formats;

namespace DragoAnt.Observer;

/// <summary>
/// Format-neutral masking rules: one declaration masks the same members in every format. A
/// <see cref="MaskRulesBuilder.Match"/> rule tests a member's own name at any depth, a
/// <see cref="MaskRulesBuilder.Path"/> rule the levels from the root, an array item being a level. A Path rule wins
/// over a Match rule; among rules of one kind the first that matches wins. Build it once and share it: it does not
/// change.
/// </summary>
[Experimental(ExperimentalIds.MaskRules, UrlFormat = ExperimentalIds.Url)]
public sealed class MaskRules
{
    internal MaskRules(IReadOnlyList<MaskRule> rules, ValuePolicy unmatched)
    {
        Rules = rules;
        Unmatched = unmatched;
    }

    /// <summary>Creates the rules.</summary>
    /// <param name="rules">Adds the rules, each finished with <see cref="MaskRuleBuilder.Mask"/> or <see cref="MaskRuleBuilder.Unmasked"/>.</param>
    /// <param name="unmatched">Policy for values no rule matches; <see cref="ValuePolicy.AllowList"/> when <c>null</c>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rules"/> is <c>null</c>.</exception>
    /// <exception cref="InvalidOperationException">A rule was started and not finished.</exception>
    public static MaskRules Create(Action<MaskRulesBuilder> rules, ValuePolicy? unmatched = null)
    {
        ArgumentNullException.ThrowIfNull(rules);
        var builder = new MaskRulesBuilder();
        rules(builder);
        return builder.Build(unmatched ?? ValuePolicy.AllowList);
    }

    /// <summary>Policy for values no rule matches.</summary>
    public ValuePolicy Unmatched { get; }

    /// <summary>The rules in declaration order. For format implementations.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public IReadOnlyList<MaskRule> Rules { get; }
}

/// <summary>
/// Adds the rules of a <see cref="MaskRules"/>; finish each with <see cref="MaskRuleBuilder.Mask"/> or
/// <see cref="MaskRuleBuilder.Unmasked"/> before starting the next.
/// </summary>
[Experimental(ExperimentalIds.MaskRules, UrlFormat = ExperimentalIds.Url)]
public sealed class MaskRulesBuilder
{
    private readonly List<MaskRule> _rules = [];
    private MaskRuleBuilder? _open;
    private bool _built;

    internal MaskRulesBuilder() { }

    /// <summary>Starts a rule for every member with this name, at any depth.</summary>
    /// <param name="name">Name test: a string for an exact name, or one of <see cref="Names"/>.</param>
    /// <exception cref="InvalidOperationException">The previous rule is not finished, or the rules are built.</exception>
    public MaskRuleBuilder Match(NameMatch name) => Start(MaskRuleScope.AnyDepth, [name]);

    /// <summary>
    /// Starts a rule for the members at these levels from the root, one name test per level; an array item is a level,
    /// matched by a test that accepts <c>null</c>.
    /// </summary>
    /// <param name="levels">Name tests, one per level from the root.</param>
    /// <exception cref="ArgumentNullException"><paramref name="levels"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException">No level was supplied.</exception>
    /// <exception cref="InvalidOperationException">The previous rule is not finished, or the rules are built.</exception>
    public MaskRuleBuilder Path(params NameMatch[] levels)
    {
        ArgumentNullException.ThrowIfNull(levels);
        if (levels.Length == 0)
        {
            throw new ArgumentException("A path needs at least one level.", nameof(levels));
        }

        return Start(MaskRuleScope.FromRoot, [.. levels]);
    }

    internal void Finish(MaskRuleBuilder rule, MaskRuleAction action, MaskTag tag)
    {
        if (_open != rule || rule.Finished)
        {
            throw new InvalidOperationException("The rule is finished.");
        }

        rule.Finished = true;
        _rules.Add(new MaskRule(rule.Scope, rule.Levels, action, tag, rule.CommentKinds, rule.CommentRule));
        _open = null;
    }

    internal MaskRules Build(ValuePolicy unmatched)
    {
        if (_open is not null)
        {
            throw new InvalidOperationException("A rule was started and not finished.");
        }

        _built = true;
        return new MaskRules(_rules.AsReadOnly(), unmatched);
    }

    private MaskRuleBuilder Start(MaskRuleScope scope, NameMatch[] levels)
    {
        if (_built || _open is not null)
        {
            throw new InvalidOperationException("The previous rule is not finished, or the rules are built.");
        }

        return _open = new MaskRuleBuilder(this, scope, levels);
    }
}

/// <summary>A rule in progress: says what happens to the values it matches.</summary>
[Experimental(ExperimentalIds.MaskRules, UrlFormat = ExperimentalIds.Url)]
public sealed class MaskRuleBuilder
{
    private readonly MaskRulesBuilder _owner;
    internal MaskRuleScope Scope { get; }
    internal NameMatch[] Levels { get; }
    internal CommentKind CommentKinds { get; private set; }
    internal CommentRule? CommentRule { get; private set; }
    internal bool Finished { get; set; }

    internal MaskRuleBuilder(MaskRulesBuilder owner, MaskRuleScope scope, NameMatch[] levels)
        => (_owner, Scope, Levels) = (owner, scope, levels);

    /// <summary>
    /// Masks the matched values with the call's strategy and <paramref name="tag"/>; a value in the no-value form stays
    /// so, and an object or array is masked whole.
    /// </summary>
    /// <param name="tag">How the matched values are masked.</param>
    /// <exception cref="InvalidOperationException">The rule is finished.</exception>
    public MaskRulesBuilder Mask(MaskTag tag)
    {
        _owner.Finish(this, MaskRuleAction.Mask, tag);
        return _owner;
    }

    /// <summary>
    /// Writes the matched strings, numbers and booleans unchanged; an object or array goes on to the next matching rule
    /// or the unmatched policy.
    /// </summary>
    /// <exception cref="InvalidOperationException">The rule is finished.</exception>
    public MaskRulesBuilder Unmasked()
    {
        _owner.Finish(this, MaskRuleAction.Unmasked, MaskTag.Full);
        return _owner;
    }

    /// <summary>
    /// Decides the comments the matched members own, for the placements in <paramref name="kinds"/>, instead of the
    /// call's <see cref="ObserverOptions.Comments"/> policy. A kept comment of a masked value is still written masked
    /// unless <paramref name="rule"/> calls <see cref="CommentContext.Raw"/>.
    /// </summary>
    /// <param name="kinds">Comment placements the rule decides.</param>
    /// <param name="rule">What to do with each matching comment.</param>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="kinds"/> names no placement.</exception>
    /// <exception cref="InvalidOperationException">The rule already has a comment rule, or is finished.</exception>
    public MaskRuleBuilder Comment(CommentKind kinds, CommentRule rule)
    {
        if (Finished || CommentRule is not null)
        {
            throw new InvalidOperationException("The rule already has a comment rule, or is finished.");
        }

        if (kinds == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(kinds));
        }

        ArgumentNullException.ThrowIfNull(rule);
        CommentKinds = kinds;
        CommentRule = rule;
        return this;
    }
}

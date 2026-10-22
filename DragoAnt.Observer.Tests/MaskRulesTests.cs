using DragoAnt.Observer.Formats;

namespace DragoAnt.Observer.Tests;

public sealed class MaskRulesTests
{
    [Fact]
    public void Create_KeepsTheRulesInOrder()
    {
        var anyItem = new NameMatch(n => n is null);
        var rules = MaskRules.Create(b => b
            .Match("password").Comment(CommentKind.Any, CommentRules.Drop).Mask(MaskTag.Full)
            .Path("card", "number").Mask(MaskTag.Last4)
            .Path("lines", anyItem, "qty").Unmasked(), ValuePolicy.BlockList);

        rules.Unmatched.Should().BeSameAs(ValuePolicy.BlockList);
        rules.Rules.Select(r => (r.Scope, r.Levels.Count, r.Action, r.Tag, r.CommentKinds)).Should().Equal(
            (MaskRuleScope.AnyDepth, 1, MaskRuleAction.Mask, MaskTag.Full, CommentKind.Any),
            (MaskRuleScope.FromRoot, 2, MaskRuleAction.Mask, MaskTag.Last4, (CommentKind)0),
            (MaskRuleScope.FromRoot, 3, MaskRuleAction.Unmasked, MaskTag.Full, (CommentKind)0));
        rules.Rules[0].CommentRule.Should().BeSameAs(CommentRules.Drop);
        rules.Rules[1].CommentRule.Should().BeNull();
        rules.Rules[0].ToString().Should().Be($"Match({(NameMatch)"password"}) → Mask(Full)");
        rules.Rules[2].ToString().Should().Be($"Path({(NameMatch)"lines"}, {anyItem}, {(NameMatch)"qty"}) → Unmasked");
    }

    [Fact]
    public void Create_DefaultsToAllowList() => MaskRules.Create(_ => { }).Unmatched.Should().BeSameAs(ValuePolicy.AllowList);

    [Fact]
    public void Path_CopiesItsLevels()
    {
        NameMatch[] levels = ["card", "number"];
        var rules = MaskRules.Create(b => b.Path(levels).Mask(MaskTag.Full));
        levels[0] = "other";
        rules.Rules[0].Levels[0].ToString().Should().Be(((NameMatch)"card").ToString());
    }

    [Fact]
    public void Levels_CannotBeChangedThroughACast()
    {
        var levels = MaskRules.Create(b => b.Path("card", "number").Mask(MaskTag.Full)).Rules[0].Levels;
        levels.Should().NotBeAssignableTo<NameMatch[]>();
        levels.Should().BeAssignableTo<ICollection<NameMatch>>().Which.IsReadOnly.Should().BeTrue();
    }

    [Fact]
    public void Misuse_Throws()
    {
        FluentActions.Invoking(() => MaskRules.Create(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => MaskRules.Create(b => b.Path())).Should().Throw<ArgumentException>();
        FluentActions.Invoking(() => MaskRules.Create(b => b.Path(null!))).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => MaskRules.Create(b => b.Match("a"))).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => MaskRules.Create(b => { b.Match("a"); b.Match("b").Mask(MaskTag.Full); })).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => MaskRules.Create(b => { var r = b.Match("a"); r.Mask(MaskTag.Full); r.Unmasked(); })).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => MaskRules.Create(b => b.Match("a").Comment(CommentKind.Before, CommentRules.Drop).Comment(CommentKind.Inline, CommentRules.Keep).Mask(MaskTag.Full))).Should().Throw<InvalidOperationException>();
        FluentActions.Invoking(() => MaskRules.Create(b => b.Match("a").Comment((CommentKind)0, CommentRules.Drop).Mask(MaskTag.Full))).Should().Throw<ArgumentOutOfRangeException>();
        FluentActions.Invoking(() => MaskRules.Create(b => b.Match("a").Comment(CommentKind.Any, null!).Mask(MaskTag.Full))).Should().Throw<ArgumentNullException>();
        MaskRulesBuilder? kept = null;
        MaskRules.Create(b => kept = b);
        FluentActions.Invoking(() => kept!.Match("late")).Should().Throw<InvalidOperationException>();
    }
}

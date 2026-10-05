namespace DragoAnt.Observer.Tests;

public sealed class CommentPolicyTests
{
    private static (CommentAction Action, MaskTag Tag, string? Replacement) Decide(CommentPolicy policy, CommentRule? rule, bool ownerMasked)
    {
        var path = new DataPath(default, 1, true);
        try
        {
            path.PushName("password"u8);
            var context = new CommentContext("rotated"u8, CommentKind.Inline, CommentStyle.Line, in path, ownerMasked);
            policy.Decide(ref context, rule);
            return (context.Action, context.MaskTag, context.Replacement);
        }
        finally
        {
            path.Dispose();
        }
    }

    [Theory]
    [InlineData(CommentPolicyKind.AllowList, false, CommentAction.Drop)]
    [InlineData(CommentPolicyKind.AllowList, true, CommentAction.Drop)]
    [InlineData(CommentPolicyKind.BlockList, false, CommentAction.Keep)]
    [InlineData(CommentPolicyKind.BlockList, true, CommentAction.Mask)]
    [InlineData(CommentPolicyKind.Mask, false, CommentAction.Mask)]
    [InlineData(CommentPolicyKind.DropAll, true, CommentAction.Drop)]
    public void Policies_WithoutRules(CommentPolicyKind kind, bool ownerMasked, CommentAction expected)
    {
        var policy = kind switch
        {
            CommentPolicyKind.AllowList => CommentPolicy.AllowList,
            CommentPolicyKind.BlockList => CommentPolicy.BlockList,
            CommentPolicyKind.Mask => CommentPolicy.MaskAll,
            _ => CommentPolicy.DropAll,
        };

        Decide(policy, null, ownerMasked).Action.Should().Be(expected);
    }

    [Fact]
    public void RuleOverridesThePolicy_ButNotDropAll()
    {
        Decide(CommentPolicy.AllowList, CommentRules.Keep, ownerMasked: false).Action.Should().Be(CommentAction.Keep);
        Decide(CommentPolicy.BlockList, CommentRules.Drop, ownerMasked: false).Action.Should().Be(CommentAction.Drop);
        Decide(CommentPolicy.DropAll, CommentRules.Raw, ownerMasked: false).Action.Should().Be(CommentAction.Drop);
    }

    [Fact]
    public void MaskedOwner_KeepMasks_RawDoesNot()
    {
        Decide(CommentPolicy.AllowList, CommentRules.Keep, ownerMasked: true).Should().Be((CommentAction.Mask, MaskTag.Full, (string?)null));
        Decide(CommentPolicy.AllowList, CommentRules.Raw, ownerMasked: true).Action.Should().Be(CommentAction.Raw);
    }

    [Fact]
    public void MaskAndReplace_CarryTheirArguments()
    {
        Decide(CommentPolicy.Mask(MaskTag.Hash), null, ownerMasked: false).Should().Be((CommentAction.Mask, MaskTag.Hash, (string?)null));
        Decide(CommentPolicy.AllowList, CommentRules.Mask(MaskTag.Last4), ownerMasked: false).Tag.Should().Be(MaskTag.Last4);
        Decide(CommentPolicy.AllowList, (ref CommentContext c) => c.Replace("x"), ownerMasked: true).Should().Be((CommentAction.Replace, MaskTag.Full, "x"));
    }

    [Fact]
    public void Context_ExposesTheComment()
    {
        var path = new DataPath(default, 1, true);
        try
        {
            path.PushName("a"u8);
            var context = new CommentContext("t"u8, CommentKind.Before, CommentStyle.Block, in path, true);

            context.Text.ToArray().Should().Equal("t"u8.ToArray());
            context.Kind.Should().Be(CommentKind.Before);
            context.Style.Should().Be(CommentStyle.Block);
            context.Owner.ToString().Should().Be("a");
            context.OwnerMasked.Should().BeTrue();
            context.Action.Should().Be(CommentAction.Drop);
        }
        finally
        {
            path.Dispose();
        }
    }

    [Fact]
    public void Names()
    {
        CommentPolicy.AllowList.ToString().Should().Be("AllowList");
        CommentPolicy.MaskAll.ToString().Should().Be("MaskAll");
        CommentPolicy.Mask(MaskTag.Hash).ToString().Should().Be("Mask(Hash)");
        ObserverOptions.Default.Comments.Should().BeSameAs(CommentPolicy.AllowList);
        CommentKind.Any.Should().Be(CommentKind.Before | CommentKind.After | CommentKind.Inline);
    }
}

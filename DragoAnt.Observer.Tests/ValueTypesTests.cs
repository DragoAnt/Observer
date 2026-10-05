namespace DragoAnt.Observer.Tests;

public sealed class ValueTypesTests
{
    [Fact]
    public void MaskTag_FactoriesAndEquality()
    {
        MaskTag.Full.Kind.Should().Be(MaskKind.Full);
        MaskTag.Last4.Kind.Should().Be(MaskKind.Last4);
        MaskTag.Hash.Kind.Should().Be(MaskKind.Hash);
        MaskTag.Null.Kind.Should().Be(MaskKind.Null);
        ((MaskTag)MaskKind.Hash).Should().Be(MaskTag.Hash);
        default(MaskTag).Should().Be(MaskTag.Full);

        var custom = MaskTag.Custom("email");
        custom.Kind.Should().Be(MaskKind.Custom);
        custom.TryGetKey<string>(out var key).Should().BeTrue();
        key.Should().Be("email");
        custom.TryGetKey<int>(out _).Should().BeFalse();
        custom.Should().Be(MaskTag.Create(MaskKind.Custom, "email"));
        (custom == MaskTag.Custom("email")).Should().BeTrue();
        (custom != MaskTag.Custom("phone")).Should().BeTrue();
        custom.GetHashCode().Should().Be(MaskTag.Custom("email").GetHashCode());
        custom.Equals((object)MaskTag.Custom("email")).Should().BeTrue();
        custom.Equals("email").Should().BeFalse();
        custom.ToString().Should().Be("Custom (email)");
        MaskTag.Hash.WithKey("pii").Should().Be(MaskTag.Create(MaskKind.Hash, "pii"));
        MaskTag.Hash.ToString().Should().Be("Hash");
        FluentActions.Invoking(() => MaskTag.Custom(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ValuePolicy_Kinds()
    {
        ValuePolicy.BlockList.Kind.Should().Be(ValuePolicyKind.BlockList);
        ValuePolicy.AllowList.Kind.Should().Be(ValuePolicyKind.AllowList);
        ValuePolicy.NullList.Kind.Should().Be(ValuePolicyKind.NullList);
        var tagged = ValuePolicy.Tagged(MaskTag.Hash);
        tagged.Kind.Should().Be(ValuePolicyKind.Tagged);
        tagged.Tag.Should().Be(MaskTag.Hash);
        tagged.ToString().Should().Be("Tagged(Hash)");
        ValuePolicy.AllowList.ToString().Should().Be("AllowList");
    }

    [Fact]
    public void MaskResult_HasValueEquality()
    {
        var result = new MaskResult { Status = MaskStatus.Truncated, BytesWritten = 3, FailedAtByte = 7, Flags = MaskFlags.InputTruncated | MaskFlags.ValueCut };

        result.Should().Be(result with { });
        result.Should().NotBe(result with { Flags = MaskFlags.None });
        default(MaskResult).Status.Should().Be(MaskStatus.Masked);
    }

    [Fact]
    public void PathExplanation_ToString()
        => new PathExplanation { Path = "a.b", Outcome = PathOutcome.Masked, Rule = "Path(\"a\", \"b\")", Action = "Mask(\"***\")" }
            .ToString().Should().Be("a.b: Masked by Path(\"a\", \"b\") → Mask(\"***\")");

    [Fact]
    public void Options_Defaults()
    {
        var options = ObserverOptions.Default;

        options.MaxOutputBytes.Should().Be(int.MaxValue);
        options.MaxValueBytes.Should().Be(int.MaxValue);
        options.MaxDepth.Should().Be(64);
        options.HashKey.IsEmpty.Should().BeTrue();
        options.HashKeyId.Should().BeNull();
        options.Strategy.Should().BeNull();
        options.NameCaseInsensitive.Should().BeTrue();
        options.IgnoreNulls.Should().BeFalse();
        NoContext.Instance.Should().BeSameAs(NoContext.Instance);
    }

    [Fact]
    public void WithBase64HashKey_KeepsTheDerivedType()
    {
        var key = Convert.ToBase64String(Enumerable.Range(1, 33).Select(i => (byte)i).ToArray());
        var options = new DerivedOptions { Extra = 5, HashKeyId = 9 };

        var copy = options.WithBase64HashKey(key);

        copy.Should().BeOfType<DerivedOptions>();
        copy.Extra.Should().Be(5);
        copy.HashKeyId.Should().Be(9);
        copy.HashKey.ToArray().Should().Equal(Convert.FromBase64String(key));
        options.HashKey.IsEmpty.Should().BeTrue();
    }

    [Theory]
    [InlineData("c2hvcnQ=")]
    [InlineData("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!")]
    public void WithBase64HashKey_RejectsBadKeys(string key)
        => FluentActions.Invoking(() => ObserverOptions.Default.WithBase64HashKey(key)).Should().Throw<ArgumentException>();

    [Fact]
    public void WithBase64HashKey_RejectsNull()
    {
        FluentActions.Invoking(() => ObserverOptions.Default.WithBase64HashKey(null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => ((ObserverOptions)null!).WithBase64HashKey("x")).Should().Throw<ArgumentNullException>();
    }

    private sealed record DerivedOptions : ObserverOptions
    {
        public int Extra { get; init; }
    }
}

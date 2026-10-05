using System.Text;
using System.Text.RegularExpressions;

namespace DragoAnt.Observer.Tests;

public sealed class NameMatchTests
{
    private static bool Match(NameMatch match, string name, bool ignoreCase = true)
    {
        var path = new DataPath(default, 1, ignoreCase);
        try
        {
            path.PushName(Encoding.UTF8.GetBytes(name));
            return match.IsMatch(in path, 0);
        }
        finally
        {
            path.Dispose();
        }
    }

    public static TheoryData<string, string, bool, bool> Cases => new()
    {
        { "exact", "driverLicense", true, true },
        { "exact", "DRiverLicensE", true, true },
        { "exact", "DRiverLicensE", false, false },
        { "exact", "drіverLicense", true, false },
        { "starts", "PassWord1", true, true },
        { "starts", "PassWord1", false, false },
        { "ends", "accessTOKEN", true, true },
        { "ends", "accessTOKEN", false, false },
        { "contains", "myPinCode", true, true },
        { "oneof", "SSN", true, true },
        { "oneof", "SSN", false, false },
        { "regex", "DRiverLicensE", true, true },
        { "regex", "DRiverLicensE", false, false },
        { "regexIgnoreCase", "DRiverLicensE", false, true },
        { "func", "DRiverLicensE", true, true },
        { "func", "DRiverLicensE", false, false },
        { "nonAscii", "ПАРОЛЬ", true, true },
        { "nonAscii", "ПАРОЛЬ", false, false },
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryTest_FollowsTheCaseOption(string test, string name, bool ignoreCase, bool expected)
    {
        NameMatch match = test switch
        {
            "exact" => "driverLicense",
            "starts" => Names.StartsWith("password"),
            "ends" => Names.EndsWith("token"),
            "contains" => Names.Contains("pin"),
            "oneof" => Names.OneOf("ssn", "pin"),
            "regex" => Names.Regex(new Regex("^driverLicense$")),
            "regexIgnoreCase" => Names.Regex(new Regex("^driverLicense$", RegexOptions.IgnoreCase)),
            "func" => new NameMatch((n, c) => string.Equals(n, "driverLicense", c)),
            "nonAscii" => "пароль",
            _ => throw new ArgumentOutOfRangeException(nameof(test)),
        };

        Match(match, name, ignoreCase).Should().Be(expected);
        match.IsMatch(name, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal).Should().Be(expected);
    }

    [Fact]
    public void CaseBlindFunction_DecidesOnItsOwn()
    {
        var match = new NameMatch(n => n == "x");

        Match(match, "x").Should().BeTrue();
        Match(match, "X").Should().BeFalse();
    }

    [Fact]
    public void Default_MatchesNothing()
    {
        Match(default, "a").Should().BeFalse();
        default(NameMatch).ToString().Should().Be("no name");
    }

    [Fact]
    public void ArrayItem_HasNoName()
    {
        var path = new DataPath(default, 1, true);
        try
        {
            path.PushItem(0);
            ((NameMatch)"a").IsMatch(in path, 0).Should().BeFalse();
            Names.OneOf("a").IsMatch(in path, 0).Should().BeFalse();
            new NameMatch((n, _) => n is null).IsMatch(in path, 0).Should().BeTrue();
        }
        finally
        {
            path.Dispose();
        }
    }

    [Fact]
    public void ToString_DescribesTheTest()
    {
        ((NameMatch)"card").ToString().Should().Be("\"card\"");
        Names.StartsWith("x-").ToString().Should().Be("StartsWith(\"x-\")");
        Names.OneOf("a", "b").ToString().Should().Be("OneOf(\"a\", \"b\")");
        Names.Regex(new Regex("^a$")).ToString().Should().Be("Regex(/^a$/)");
        new NameMatch(_ => true).ToString().Should().Be("custom name test");
    }

    [Fact]
    public void NullArguments_Throw()
    {
        FluentActions.Invoking(() => new NameMatch((Func<string?, bool>)null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => new NameMatch((Func<string?, StringComparison, bool>)null!)).Should().Throw<ArgumentNullException>();
        FluentActions.Invoking(() => Names.Regex(null!)).Should().Throw<ArgumentNullException>();
    }
}

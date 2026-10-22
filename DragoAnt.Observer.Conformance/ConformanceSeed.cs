using static DragoAnt.Observer.ValuePolicy;

#pragma warning disable DAOEXP001, DAOEXP002

namespace DragoAnt.Observer.Conformance;

internal static class ConformanceSeed
{
    private const string S1 = "ZQS1abc1def1ghi1";
    private const string S2 = "ZQS2abc2def2ghi2";
    private const string S3 = "ZQS3abc3def3ghi3";
    private const string S4 = "ZQS4abc4def4ghi4";
    private const string S5 = "ZQS5abc5def5ghi5";
    private const string S6 = "ZQS6abc6def6ghi6";
    private const string S7 = "ZQS7abc7def7ghi7";
    private const string S8 = "ZQS8abc8def8ghi8";
    private const string S9 = "ZQS9abc9def9ghi9";
    private const string S10 = "ZQSAabcAdefAghiA";
    private const string S11 = "ZQSBabcBdefBghiB";
    private const string S12 = "ZQSCabcCdefCghiC";
    private const string S13 = "ZQSDabcDdefDghiD";
    private const string S14 = "ZQSEabcEdefEghiE";
    private const string S21 = "ZQSFabcFdefFghiF";
    private const string S22 = "ZQSGabcGdefGghiG";
    private const string S23 = "ZQSHabcHdefHghiH";
    private const string S24 = "ZQSIabcIdefIghiI";
    private const string S31 = "ZQSJabcJdefJghiJ";
    private const string S32 = "ZQSKabcKdefKghiK";
    private const string S33 = "ZQSLabcLdefLghiL";
    private const string S34 = "ZQSMabcMdefMghiM";
    private const string S40 = "ZQSNabcNdefNghiN";
    private const string K1 = "ZQK0000000000001";
    private const string K2 = "ZQK0000000000002";
    private const string K3 = "ZQK0000000000003";
    private const string K4 = "ZQK0000000000004";
    private const string K5 = "ZQK0000000000005";
    private const string K6 = "ZQK0000000000006";

    public static IReadOnlyList<ConformanceScenario> Create()
    {
        var common = Document(S1, S2, S3, K1);
        return
        [
            Scenario("one-declaration", Rules(BlockList), common,
                [Secret("password", S1, MaskTag.Full), Secret("card.number", S2, MaskTag.Last4), Secret("email", S3, MaskTag.Hash)], [K1]),
            Scenario("policy-allowlist", Rules(AllowList), Document(S11, S12, S13, S14),
                [Secret("password", S11, MaskTag.Full), Secret("card.number", S12, MaskTag.Last4), Secret("email", S13, MaskTag.Hash), Secret("user", S14, MaskTag.Full)]),
            Scenario("policy-nulllist", Rules(NullList), Document(S21, S22, S23, S24),
                [Secret("password", S21, MaskTag.Full), Secret("card.number", S22, MaskTag.Last4), Secret("email", S23, MaskTag.Hash), Secret("user", S24, MaskTag.Null)]),
            Scenario("policy-tagged", Rules(Tagged(MaskTag.Hash)), Document(S31, S32, S33, S34),
                [Secret("password", S31, MaskTag.Full), Secret("card.number", S32, MaskTag.Last4), Secret("email", S33, MaskTag.Hash), Secret("user", S34, MaskTag.Hash)]),
            Scenario("case", MaskRules.Create(b => b.Match("password").Mask(MaskTag.Full).Path("card", "number").Mask(MaskTag.Full), BlockList),
                $"{{\"PASSWORD\":\"{S4}\",\"Card\":{{\"Number\":\"{S5}\"}},\"other\":\"{K2}\"}}",
                [Secret("PASSWORD", S4, MaskTag.Full), Secret("Card.Number", S5, MaskTag.Full)], [K2]),
            Scenario("path-and-depth", MaskRules.Create(b => b.Path("user", "ssn").Mask(MaskTag.Full).Match("token").Mask(MaskTag.Full), BlockList),
                $"{{\"user\":{{\"ssn\":\"{S6}\"}},\"other\":{{\"user\":{{\"ssn\":\"{K3}\"}}}},\"a\":{{\"b\":{{\"token\":\"{S7}\"}}}}}}",
                [Secret("user.ssn", S6, MaskTag.Full), Secret("a.b.token", S7, MaskTag.Full)], [K3]),
            Scenario("comments", MaskRules.Create(b => b.Match("password").Mask(MaskTag.Full), BlockList),
                $"{{\"password\":\"{S8}\", // {S9}\n\"user\":\"{K4}\"}}",
                [Secret("password", S8, MaskTag.Full), Secret("comment", S9, MaskTag.Null)], [K4], needsComments: true),
            Scenario("untouched-blocklist", MaskRules.Create(_ => { }, BlockList),
                """{"a":"ZQK0000000000005","b":"ZQK0000000000006"}""", [], [K5, K6]),
            Scenario("truncation", MaskRules.Create(b => b.Match("password").Mask(MaskTag.Null), BlockList),
                $"{{\"value\":\"{K1}\",\"password\":\"{S40}\"}}", [Secret("password", S40, MaskTag.Null)], status: MaskStatus.Truncated,
                flags: MaskFlags.OutputCapped, options: ConformanceKit.DefaultOptions with { MaxOutputBytes = 8 }),
            Scenario("invalid", MaskRules.Create(b => b.Match("password").Mask(MaskTag.Full), BlockList),
                $"{{\"password\":\"{S10}\",\"x\":]", [Secret("password", S10, MaskTag.Full)], status: MaskStatus.Invalid),
            Scenario("empty", MaskRules.Create(b => b.Match("password").Mask(MaskTag.Full), BlockList), "", [], status: MaskStatus.Unrecognized),
        ];
    }

    private static MaskRules Rules(ValuePolicy unmatched)
        => MaskRules.Create(b => b.Match("password").Mask(MaskTag.Full).Path("card", "number").Mask(MaskTag.Last4).Match("email").Mask(MaskTag.Hash), unmatched);

    private static string Document(string password, string card, string email, string user)
        => $"{{\"user\":\"{user}\",\"password\":\"{password}\",\"card\":{{\"number\":\"{card}\"}},\"email\":\"{email}\"}}";

    private static ConformanceSecret Secret(string path, string clear, MaskTag tag) => new(path, clear, tag);

    private static ConformanceScenario Scenario(string name, MaskRules rules, string json,
        IReadOnlyList<ConformanceSecret> secrets, IReadOnlyList<string>? visible = null,
        bool needsComments = false, MaskStatus status = MaskStatus.Masked, MaskFlags flags = MaskFlags.None,
        ObserverOptions? options = null)
        => new(name, rules)
        {
            Options = options ?? ConformanceKit.DefaultOptions,
            Secrets = secrets,
            Visible = visible ?? [],
            NeedsComments = needsComments,
            Documents = new Dictionary<string, ConformanceDocument> { ["json"] = new(json, status, flags) },
        };
}

#pragma warning restore DAOEXP001, DAOEXP002

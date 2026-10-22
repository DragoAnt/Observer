using System.Diagnostics.CodeAnalysis;

namespace DragoAnt.Observer.Conformance;

/// <summary>One rule declaration with what it must hide and keep, and a document per format, keyed by format name.</summary>
/// <param name="Name">Stable scenario name.</param>
/// <param name="Rules">Format-neutral rules.</param>
[Experimental("DAOEXP002", UrlFormat = "https://github.com/DragoAnt/Observer/blob/main/docs/experimental.md")]
public sealed record ConformanceScenario(string Name, MaskRules Rules)
{
    /// <summary>Options used for the scenario.</summary>
    public ObserverOptions Options { get; init; } = ConformanceKit.DefaultOptions;
    /// <summary>Values the rules mask: none may appear in output, and each tag's replacement must.</summary>
    public IReadOnlyList<ConformanceSecret> Secrets { get; init; } = [];
    /// <summary>Values the rules keep: each appears in the output of the whole document.</summary>
    public IReadOnlyList<string> Visible { get; init; } = [];
    /// <summary>Documents keyed by format name.</summary>
    public IReadOnlyDictionary<string, ConformanceDocument> Documents { get; init; } = new Dictionary<string, ConformanceDocument>();
    /// <summary>Run only for a format whose traits say it has comments.</summary>
    public bool NeedsComments { get; init; }
}

/// <summary>A value a rule masks, where it is, and the tag that masks it.</summary>
/// <param name="Path">Format-neutral path.</param>
/// <param name="Clear">Clear value that must not leak: no 8 consecutive bytes of it (all of it, when shorter) may appear in output.</param>
/// <param name="Tag">Expected masking tag.</param>
[Experimental("DAOEXP002", UrlFormat = "https://github.com/DragoAnt/Observer/blob/main/docs/experimental.md")]
public sealed record ConformanceSecret(string Path, string Clear, MaskTag Tag);

/// <summary>The scenario's document in one format, and the status and flags masking all of it reports.</summary>
/// <param name="Text">Document text.</param>
/// <param name="Status">Expected status.</param>
/// <param name="Flags">Expected flags.</param>
[Experimental("DAOEXP002", UrlFormat = "https://github.com/DragoAnt/Observer/blob/main/docs/experimental.md")]
public sealed record ConformanceDocument(string Text, MaskStatus Status, MaskFlags Flags = MaskFlags.None);

/// <summary>What a format promises, so the kit checks only what applies.</summary>
[Experimental("DAOEXP002", UrlFormat = "https://github.com/DragoAnt/Observer/blob/main/docs/experimental.md")]
public sealed record ConformanceTraits
{
    /// <summary>Whether the format has comments.</summary>
    public bool Comments { get; init; }
    /// <summary>Every token no rule or policy changes is copied byte for byte.</summary>
    public bool RoundTripsUntouchedInput { get; init; }
    /// <summary>The format reads many records from one input; sync-and-async is left to its own tests.</summary>
    public bool MultiRecord { get; init; }
    /// <summary>How a string value appears in output, for example percent-encoded; identity by default.</summary>
    public Func<string, string> EncodeValue { get; init; } = static s => s;
}

/// <summary>One invariant one document broke.</summary>
/// <param name="Scenario">Scenario name.</param>
/// <param name="Format">Format name.</param>
/// <param name="Invariant">Stable invariant id.</param>
/// <param name="Detail">Failure detail.</param>
[Experimental("DAOEXP002", UrlFormat = "https://github.com/DragoAnt/Observer/blob/main/docs/experimental.md")]
public sealed record ConformanceFailure(string Scenario, string Format, string Invariant, string Detail);

/// <summary>What a run found, and how much it checked: zero scenarios run is a failed run, not a clean one.</summary>
/// <param name="Failures">Invariant failures.</param>
/// <param name="ScenariosRun">Selected scenarios.</param>
/// <param name="ChecksRun">Individual invariant checks.</param>
[Experimental("DAOEXP002", UrlFormat = "https://github.com/DragoAnt/Observer/blob/main/docs/experimental.md")]
public sealed record ConformanceReport(IReadOnlyList<ConformanceFailure> Failures, int ScenariosRun, int ChecksRun);

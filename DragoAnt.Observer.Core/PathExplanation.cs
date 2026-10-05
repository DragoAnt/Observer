namespace DragoAnt.Observer;

/// <summary>
/// What an observer does with a value, as reported by its <c>Explain</c> method.
/// </summary>
public enum PathOutcome
{
    /// <summary>
    /// The value is written as it is.
    /// </summary>
    Unchanged,

    /// <summary>
    /// The value is replaced: masked, hashed, written as <c>null</c>, or an object or array masked whole.
    /// </summary>
    Masked,

    /// <summary>
    /// The value is handed to the context by a read rule and written as it is.
    /// </summary>
    Read,

    /// <summary>
    /// A custom rule or policy decides; the observer cannot tell what it writes.
    /// </summary>
    Custom,

    /// <summary>
    /// A payload with this structure is not masked at all: its status is <see cref="MaskStatus.Invalid"/>.
    /// </summary>
    Invalid,
}

/// <summary>
/// Which rule or policy of an observer handles a path, and what it does with the value there.
/// </summary>
public sealed record PathExplanation
{
    /// <summary>
    /// The path explained, normalized, for example <c>lines[0].qty</c>.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// What happens to the value.
    /// </summary>
    public required PathOutcome Outcome { get; init; }

    /// <summary>
    /// The rule or policy that decides, for example <c>Match("qty")</c> or <c>default policy AllowList</c>.
    /// </summary>
    public required string Rule { get; init; }

    /// <summary>
    /// What it does, for example <c>Mask("***")</c> or <c>writes "***"</c>.
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// How the observer gets there, one entry per level of the path.
    /// </summary>
    public IReadOnlyList<string> Steps { get; init; } = [];

    /// <summary>
    /// One line, for example <c>lines[0].qty: Masked by Match("qty") → Mask("***")</c>.
    /// </summary>
    public override string ToString() => $"{Path}: {Outcome} by {Rule} → {Action}";
}

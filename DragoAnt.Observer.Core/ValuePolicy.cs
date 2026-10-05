namespace DragoAnt.Observer;

/// <summary>
/// The built-in default policies a <see cref="ValuePolicy"/> stands for.
/// </summary>
public enum ValuePolicyKind
{
    /// <summary>
    /// Every value no rule matches is written unchanged.
    /// </summary>
    BlockList,

    /// <summary>
    /// Every string, number and boolean no rule matches is written as <c>"***"</c>; <c>null</c> stays <c>null</c>.
    /// </summary>
    AllowList,

    /// <summary>
    /// Every value no rule matches is written as <c>null</c>.
    /// </summary>
    NullList,

    /// <summary>
    /// Every string, number and boolean no rule matches is masked by the call's strategy with <see cref="ValuePolicy.Tag"/>;
    /// <c>null</c> stays <c>null</c>.
    /// </summary>
    Tagged,
}

/// <summary>
/// What happens to values no rule matches, for observers of any context type and any format.
/// </summary>
public sealed class ValuePolicy
{
    private ValuePolicy(ValuePolicyKind kind, MaskTag tag)
    {
        Kind = kind;
        Tag = tag;
    }

    /// <summary>
    /// Writes every value unchanged: only values matched by a rule are masked.
    /// </summary>
    public static ValuePolicy BlockList { get; } = new(ValuePolicyKind.BlockList, MaskTag.Full);

    /// <summary>
    /// Writes every string, number and boolean as <c>"***"</c> and keeps <c>null</c>: only values a rule allows are shown.
    /// The default of every observer.
    /// </summary>
    public static ValuePolicy AllowList { get; } = new(ValuePolicyKind.AllowList, MaskTag.Full);

    /// <summary>
    /// Writes every value as <c>null</c>.
    /// </summary>
    public static ValuePolicy NullList { get; } = new(ValuePolicyKind.NullList, MaskTag.Null);

    /// <summary>
    /// Masks every string, number and boolean with the call's strategy and <paramref name="tag"/>, for example every
    /// unmatched value hashed; <c>null</c> stays <c>null</c>.
    /// </summary>
    /// <param name="tag">How each value is masked.</param>
    public static ValuePolicy Tagged(MaskTag tag) => new(ValuePolicyKind.Tagged, tag);

    /// <summary>
    /// Which built-in policy this is.
    /// </summary>
    public ValuePolicyKind Kind { get; }

    /// <summary>
    /// The tag of a <see cref="ValuePolicyKind.Tagged"/> policy.
    /// </summary>
    public MaskTag Tag { get; }

    /// <summary>
    /// The policy's name, for example <c>AllowList</c> or <c>Tagged(Hash)</c>.
    /// </summary>
    public override string ToString() => Kind == ValuePolicyKind.Tagged ? $"Tagged({Tag})" : Kind.ToString();
}

/// <summary>
/// Whether a masking function is called for a <c>null</c> value.
/// </summary>
public enum MaskNulls
{
    /// <summary>
    /// <c>null</c> stays <c>null</c> without calling the function.
    /// </summary>
    Keep,

    /// <summary>
    /// The function is called with <c>null</c> and its result is written.
    /// </summary>
    Mask,
}

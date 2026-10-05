namespace DragoAnt.Observer;

/// <summary>
/// What a shape-driven observer does with a member its shape does not know.
/// </summary>
public enum UnknownMemberPolicy
{
    /// <summary>
    /// The value is masked whole, whatever its type; its content is never read.
    /// </summary>
    MaskWhole,

    /// <summary>
    /// Objects and arrays are descended so that their member names stay visible; every value inside is masked.
    /// </summary>
    Descend,

    /// <summary>
    /// The value is written as is. Only the members the shape marks as sensitive are masked.
    /// </summary>
    PassThrough,
}

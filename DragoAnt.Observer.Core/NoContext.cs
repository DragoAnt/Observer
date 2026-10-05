namespace DragoAnt.Observer;

/// <summary>
/// Context of an observer that only masks and extracts nothing.
/// </summary>
public sealed class NoContext
{
    /// <summary>
    /// The only instance.
    /// </summary>
    public static readonly NoContext Instance = new();

    private NoContext()
    {
    }
}

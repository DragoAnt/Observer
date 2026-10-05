using System.Text.RegularExpressions;

namespace DragoAnt.Observer;

/// <summary>
/// A test of a member name — a JSON property, a CSV column, an XML element. A string converts to an exact match;
/// <see cref="Names"/> has the others. Every test follows the call's case option
/// (<see cref="ObserverOptions.NameCaseInsensitive"/>), which ignores case by default.
/// </summary>
public readonly struct NameMatch
{
    private readonly NameMatcher? _matcher;

    /// <summary>
    /// Matches names with a custom test that follows the call's case option; the name is decoded to a
    /// <see cref="string"/> for it.
    /// </summary>
    /// <param name="test">
    /// Name test; receives <c>null</c> for an array item, and <see cref="StringComparison.OrdinalIgnoreCase"/> or
    /// <see cref="StringComparison.Ordinal"/> as the call's case option.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <c>null</c>.</exception>
    public NameMatch(Func<string?, StringComparison, bool> test)
    {
        ArgumentNullException.ThrowIfNull(test);
        _matcher = new NameMatcher.FuncNameMatcher(test);
    }

    /// <summary>
    /// Matches names with a custom test that decides case on its own; prefer the overload that receives the call's
    /// <see cref="StringComparison"/>, so that the case option reaches it.
    /// </summary>
    /// <param name="test">Name test; receives <c>null</c> for an array item.</param>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <c>null</c>.</exception>
    public NameMatch(Func<string?, bool> test)
    {
        ArgumentNullException.ThrowIfNull(test);
        _matcher = new NameMatcher.FuncNameMatcher((name, _) => test(name));
    }

    internal NameMatch(NameMatcher matcher)
    {
        _matcher = matcher;
    }

    private NameMatcher Matcher => _matcher ?? NameMatcher.Never;

    /// <summary>
    /// Tests the name of one level of a path, following the path's case option.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="index">Level, from 0 to <see cref="DataPath.Length"/> - 1.</param>
    /// <returns><c>true</c> when the level is a name that matches; an array item never does, except for a custom test that accepts <c>null</c>.</returns>
    public bool IsMatch(in DataPath path, int index) => Matcher.Match(in path, index);

    /// <summary>
    /// Tests a decoded name.
    /// </summary>
    /// <param name="name">The name; <c>null</c> stands for an array item.</param>
    /// <param name="comparison"><see cref="StringComparison.OrdinalIgnoreCase"/> or <see cref="StringComparison.Ordinal"/>.</param>
    public bool IsMatch(string? name, StringComparison comparison) => Matcher.MatchString(name, comparison);

    /// <summary>
    /// The test as written in a rule, for example <c>"card"</c> or <c>StartsWith("x-")</c>.
    /// </summary>
    public override string ToString() => Matcher.Describe();

    /// <summary>
    /// Matches the exact name.
    /// </summary>
    /// <param name="name">The name.</param>
    public static implicit operator NameMatch(string name) => new(NameMatcher.Exact(name));
}

/// <summary>
/// Name tests beyond the exact name a string gives.
/// </summary>
public static class Names
{
    /// <summary>
    /// Matches names that start with <paramref name="value"/>.
    /// </summary>
    /// <param name="value">Start of the name.</param>
    public static NameMatch StartsWith(string value) => new(NameMatcher.StartsWith(value));

    /// <summary>
    /// Matches names that end with <paramref name="value"/>.
    /// </summary>
    /// <param name="value">End of the name.</param>
    public static NameMatch EndsWith(string value) => new(NameMatcher.EndsWith(value));

    /// <summary>
    /// Matches names that contain <paramref name="value"/>.
    /// </summary>
    /// <param name="value">Part of the name.</param>
    public static NameMatch Contains(string value) => new(NameMatcher.Contains(value));

    /// <summary>
    /// Matches names by a regular expression. Under the default case-insensitive option a name that differs in case
    /// only also matches; a regular expression built with <see cref="RegexOptions.IgnoreCase"/> ignores case under either option.
    /// </summary>
    /// <param name="regex">The regular expression.</param>
    /// <exception cref="ArgumentNullException"><paramref name="regex"/> is <c>null</c>.</exception>
    public static NameMatch Regex(Regex regex)
    {
        ArgumentNullException.ThrowIfNull(regex);
        return new NameMatch(new NameMatcher.RegexNameMatcher(regex));
    }

    /// <summary>
    /// Matches any of the exact names.
    /// </summary>
    /// <param name="names">The names.</param>
    public static NameMatch OneOf(params string[] names) => new(NameMatcher.OneOf(names));
}

using System;
using System.Collections.Generic;

namespace DragoAnt.Observer;

/// <summary>
/// How a rule asks for a value to be masked: a <see cref="MaskKind"/> and an optional classification
/// <see cref="Key"/> that a custom strategy maps to its own masking. Create one with a factory such as
/// <see cref="Full"/>, <see cref="Last4"/> or <see cref="Custom(object)"/>.
/// </summary>
public readonly struct MaskTag : IEquatable<MaskTag>
{
    private MaskTag(MaskKind kind, object? key)
    {
        Kind = kind;
        Key = key;
    }

    /// <summary>
    /// How the value is masked. A strategy that does not know <see cref="Key"/> falls back to it, so a hash tag with a
    /// classification key is still hashed by the built-in strategy.
    /// </summary>
    public MaskKind Kind { get; }

    /// <summary>
    /// Optional classification of the value, for example a data classification of a compliance taxonomy or a redactor
    /// name, that a custom strategy maps to its own masking. It is compared with <see cref="object.Equals(object)"/>,
    /// so prefer immutable keys with value equality.
    /// </summary>
    public object? Key { get; }

    /// <summary>
    /// <see cref="MaskKind.Full"/>: the value becomes <c>"***"</c>.
    /// </summary>
    public static MaskTag Full => new(MaskKind.Full, null);

    /// <summary>
    /// <see cref="MaskKind.Last4"/>: only the last four characters are kept.
    /// </summary>
    public static MaskTag Last4 => new(MaskKind.Last4, null);

    /// <summary>
    /// <see cref="MaskKind.Hash"/>: the value becomes a keyed hash.
    /// </summary>
    public static MaskTag Hash => new(MaskKind.Hash, null);

    /// <summary>
    /// <see cref="MaskKind.Null"/>: the value becomes <c>null</c>.
    /// </summary>
    public static MaskTag Null => new(MaskKind.Null, null);

    /// <summary>
    /// A tag only a custom strategy interprets, by <paramref name="key"/>; the built-in strategy writes <c>"***"</c>.
    /// </summary>
    /// <param name="key">Classification the strategy maps to its masking.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> is <c>null</c>.</exception>
    public static MaskTag Custom(object key) => new(MaskKind.Custom, key ?? throw new ArgumentNullException(nameof(key)));

    /// <summary>
    /// A tag of any kind with an optional classification key, for example a hash that also names its classification.
    /// </summary>
    /// <param name="kind">How the value is masked when the strategy does not know <paramref name="key"/>.</param>
    /// <param name="key">Optional classification the strategy maps to its masking.</param>
    public static MaskTag Create(MaskKind kind, object? key = null) => new(kind, key);

    /// <summary>
    /// This tag with another classification key.
    /// </summary>
    /// <param name="key">The classification; <c>null</c> removes it.</param>
    public MaskTag WithKey(object? key) => new(Kind, key);

    /// <summary>
    /// Gets <see cref="Key"/> when it is a <typeparamref name="T"/>.
    /// </summary>
    /// <param name="key">The key; <c>default</c> when it is absent or of another type.</param>
    /// <typeparam name="T">Expected key type.</typeparam>
    /// <returns><c>true</c> when the key is a <typeparamref name="T"/>.</returns>
    public bool TryGetKey<T>(out T? key)
    {
        if (Key is T typed)
        {
            key = typed;
            return true;
        }

        key = default;
        return false;
    }

    /// <summary>
    /// Lets a <see cref="MaskKind"/> stand for its tag.
    /// </summary>
    /// <param name="kind">How the value is masked.</param>
    public static implicit operator MaskTag(MaskKind kind) => new(kind, null);

    /// <inheritdoc />
    public bool Equals(MaskTag other) => Kind == other.Kind && EqualityComparer<object?>.Default.Equals(Key, other.Key);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is MaskTag other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => ((int)Kind * 397) ^ (Key?.GetHashCode() ?? 0);

    /// <summary>
    /// Whether two tags have the same kind and key.
    /// </summary>
    /// <param name="left">First tag.</param>
    /// <param name="right">Second tag.</param>
    public static bool operator ==(MaskTag left, MaskTag right) => left.Equals(right);

    /// <summary>
    /// Whether two tags differ in kind or key.
    /// </summary>
    /// <param name="left">First tag.</param>
    /// <param name="right">Second tag.</param>
    public static bool operator !=(MaskTag left, MaskTag right) => !left.Equals(right);

    /// <summary>
    /// The kind, and the key when there is one, for example <c>Hash (email)</c>.
    /// </summary>
    public override string ToString() => Key is null ? Kind.ToString() : $"{Kind} ({Key})";
}

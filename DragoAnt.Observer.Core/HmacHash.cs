using System.Buffers;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace DragoAnt.Observer;

/// <summary>
/// The text of <see cref="MaskKind.Hash"/>: HMAC-SHA256 over the value's UTF-16 code units, the first 16 bytes in
/// base64, after <c>"&lt;keyId&gt;:"</c> when a key id is set — the output format of Microsoft's <c>HmacRedactor</c>.
/// </summary>
internal static class HmacHash
{
    public const int MaxLength = 11 + 1 + EncodedLength;

    private const int HashBytes = 16;
    private const int EncodedLength = 24;
    private const int StackallocThreshold = 256;

    /// <summary>
    /// Writes the hash text of <paramref name="utf8Value"/> into <paramref name="destination"/> (at least
    /// <see cref="MaxLength"/> bytes); an empty value has an empty hash.
    /// </summary>
    public static int Write(ReadOnlySpan<byte> utf8Value, ReadOnlySpan<byte> key, int? keyId, Span<byte> destination)
    {
        if (utf8Value.IsEmpty)
        {
            return 0;
        }

        Span<byte> hash = stackalloc byte[32];
        char[]? rented = null;
        var chars = utf8Value.Length <= StackallocThreshold
            ? stackalloc char[StackallocThreshold]
            : rented = ArrayPool<char>.Shared.Rent(utf8Value.Length);
        try
        {
            var length = Encoding.UTF8.GetChars(utf8Value, chars);
            HMACSHA256.HashData(key, MemoryMarshal.AsBytes(chars[..length]), hash);
            chars[..length].Clear();
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }

        var written = 0;
        if (keyId is { } id)
        {
            id.TryFormat(destination, out written, provider: System.Globalization.CultureInfo.InvariantCulture);
            destination[written++] = (byte)':';
        }

        Base64.EncodeToUtf8(hash[..HashBytes], destination[written..], out _, out var encoded);
        return written + encoded;
    }
}

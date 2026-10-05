using System.Security.Cryptography;

namespace DragoAnt.Observer;

/// <summary>
/// Masks one sensitive value, whatever its format. One instance serves every rule of a call: the rule's
/// <see cref="MaskTag"/> says how. Strategies are synchronous and must not keep the context after the call.
/// </summary>
public abstract class ValueMaskStrategy
{
    /// <summary>
    /// Built-in strategy: <see cref="MaskKind.Full"/> writes <c>"***"</c>, <see cref="MaskKind.Last4"/> keeps the last
    /// four characters, <see cref="MaskKind.Hash"/> writes the keyed hash of a string's text or a number's or boolean's
    /// literal — the same text as Microsoft's <c>HmacRedactor</c> for the same key and key id —, <see cref="MaskKind.Null"/> writes no value;
    /// anything else, <see cref="MaskKind.Custom"/> included, becomes <c>"***"</c>.
    /// </summary>
    public static ValueMaskStrategy Default { get; } = new DefaultValueMaskStrategy();

    /// <summary>
    /// Writes the masked replacement of one value. A strategy that does not handle a tag can delegate to
    /// <see cref="Default"/>.
    /// </summary>
    /// <param name="context">The whole value, its type, the rule's tag, the call's options, the value's path and position.</param>
    /// <param name="output">Receives exactly one value.</param>
    public abstract void Mask(in MaskContext context, MaskValueWriter output);

    private sealed class DefaultValueMaskStrategy : ValueMaskStrategy
    {
        private const int Last4MinLength = 8;
        private static readonly byte[] ProcessKey = RandomNumberGenerator.GetBytes(32);

        private static ReadOnlySpan<byte> Stars => "***"u8;

        public override void Mask(in MaskContext context, MaskValueWriter output)
        {
            var scalar = context.Kind is ValueKind.String or ValueKind.Number;
            switch (context.Tag.Kind)
            {
                case MaskKind.Null:
                    output.Null();
                    break;
                case MaskKind.Last4 when scalar:
                    WriteLast4(context.Value, output);
                    break;
                case MaskKind.Hash when scalar || context.Kind == ValueKind.Boolean:
                    WriteHash(in context, output);
                    break;
                default:
                    output.String(Stars);
                    break;
            }
        }

        private static void WriteLast4(ReadOnlySpan<byte> value, MaskValueWriter output)
        {
            var start = value.Length;
            var chars = 0;
            var total = 0;
            for (var i = value.Length - 1; i >= 0; i--)
            {
                if ((value[i] & 0xC0) == 0x80)
                {
                    continue;
                }

                total++;
                if (chars < 4)
                {
                    chars++;
                    start = i;
                }
            }

            if (total < Last4MinLength)
            {
                output.String(Stars);
                return;
            }

            var tail = value[start..];
            Span<byte> masked = stackalloc byte[Stars.Length + tail.Length];
            Stars.CopyTo(masked);
            tail.CopyTo(masked[Stars.Length..]);
            output.String(masked);
        }

        private static void WriteHash(in MaskContext context, MaskValueWriter output)
        {
            var options = context.Options;
            Span<byte> text = stackalloc byte[HmacHash.MaxLength];
            var length = HmacHash.Write(context.Value, options.HashKey.IsEmpty ? ProcessKey : options.HashKey.Span, options.HashKeyId, text);
            output.String(text[..length]);
        }
    }
}

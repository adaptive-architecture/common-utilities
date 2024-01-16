using System.Buffers;
using System.Runtime.CompilerServices;

namespace AdaptArch.Common.Utilities.Encoding;

/// <summary>
/// Base class for all encoding.
/// </summary>
internal abstract class Base
{
    private const string InvalidCountOffsetOrLength = "Invalid {0}, {1} or {2} length.";

    internal byte[] Decode(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Decode(input, offset: 0, count: input.Length);
    }

    internal byte[] Decode(string input, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateParameters(input.Length, nameof(input), offset, count);

        return DecodeCore(input, offset, count);
    }

    protected abstract byte[] DecodeCore(string input, int offset, int count);

    internal string Encode(byte[] input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Encode(input, offset: 0, count: input.Length);
    }

    internal string Encode(byte[] input, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateParameters(input.Length, nameof(input), offset, count);

        return EncodeLocal(input.AsSpan(offset, count));
    }

    internal int Encode(byte[] input, int offset, char[] output, int outputOffset, int count)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ValidateParameters(input.Length, nameof(input), offset, count);
        ArgumentOutOfRangeException.ThrowIfNegative(outputOffset);
        ValidateBufferLengths(output, outputOffset, GetArraySizeRequiredToEncode(count));

        return EncodeCore(input.AsSpan(offset, count), output.AsSpan(outputOffset));
    }

    [SkipLocalsInit]
    internal string EncodeLocal(ReadOnlySpan<byte> input)
    {
        const int stackAllocThreshold = 128;

        if (input.IsEmpty)
        {
            return String.Empty;
        }

        var bufferSize = GetArraySizeRequiredToEncode(input.Length);

#pragma warning disable S1121 // SONAR: Assignments should not be made from within sub-expressions
        char[]? bufferToReturnToPool = null;
        string base32Url;
        try
        {
            var buffer = bufferSize <= stackAllocThreshold
                ? stackalloc char[stackAllocThreshold]
                : bufferToReturnToPool = ArrayPool<char>.Shared.Rent(bufferSize);

            var numBase32Chars = EncodeCore(input, buffer);
            base32Url = new string(buffer[..numBase32Chars]);
        }
        finally
        {
            if (bufferToReturnToPool != null)
            {
                ArrayPool<char>.Shared.Return(bufferToReturnToPool);
            }
        }
#pragma warning restore S1121

        return base32Url;
    }

    /// <summary>
    /// Get the minimum output <c>char[]</c> size required for encoding <paramref name="count"/> <see cref="Byte"/>s.
    /// </summary>
    /// <param name="count">The number of characters to encode.</param>
    /// <returns>
    /// The minimum output <c>char[]</c> size required for encoding <paramref name="count"/> <see cref="Byte"/>s.
    /// </returns>
    protected abstract int GetArraySizeRequiredToEncode(int count);

    protected static void ValidateBufferLengths(char[] output, int outputOffset, int count)
    {
        if (output.Length - outputOffset < count)
        {
            throw new ArgumentException(
                String.Format(InvalidCountOffsetOrLength, nameof(count), nameof(outputOffset), nameof(output)),
                nameof(count)
            );
        }
    }
    protected static void ValidateParameters(int bufferLength, string inputName, int offset, int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (bufferLength - offset < count)
        {
            throw new ArgumentException(
                String.Format(InvalidCountOffsetOrLength, nameof(count), nameof(offset), inputName),
                nameof(count)
            );
        }
    }

    protected abstract int EncodeCore(ReadOnlySpan<byte> input, Span<char> output);
}

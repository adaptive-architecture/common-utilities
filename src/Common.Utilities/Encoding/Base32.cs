using System.Buffers;
using System.Runtime.CompilerServices;

namespace AdaptArch.Common.Utilities.Encoding;

/// <summary>
/// Contains utility APIs to assist with encoding and decoding operations using <see href="https://datatracker.ietf.org/doc/html/rfc4648#section-6">Base 32</see>.
/// </summary>
/// /// <remarks>
/// Inspired by <see href="https://github.com/dotnet/aspnetcore/blob/c096dbbbe652f03be926502d790eb499682eea13/src/Identity/Extensions.Core/src/Base32.cs">AspNetCore.Identity</see>.
/// </remarks>
public static class Base32
{
    private const string InvalidCountOffsetOrLength = "Invalid {0}, {1} or {2} length.";
    private const string MalformedInput = "Malformed input: Input string contains invalid characters at index {0}.";
    private const int BitsPerChar = 5;
    private const int InputGroupSize = 8;
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>
    /// Decodes a base32 string.
    /// </summary>
    /// <param name="input">The base32 input to decode.</param>
    /// <returns>The base32url-decoded form of the input.</returns>
    /// <remarks>
    /// The input must not contain any whitespace or padding characters.
    /// Throws <see cref="FormatException"/> if the input is malformed.
    /// </remarks>
    public static byte[] Decode(string input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Decode(input, offset: 0, count: input.Length);
    }

    /// <summary>
    /// Decodes a base32 substring of a given string.
    /// </summary>
    /// <param name="input">A string containing the base32 input to decode.</param>
    /// <param name="offset">The position in <paramref name="input"/> at which decoding should begin.</param>
    /// <param name="count">The number of characters in <paramref name="input"/> to decode.</param>
    /// <returns>The base32url-decoded form of the input.</returns>
    /// <remarks>
    /// The input must not contain any whitespace or padding characters.
    /// Throws <see cref="FormatException"/> if the input is malformed.
    /// </remarks>
    public static byte[] Decode(string input, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateParameters(input.Length, nameof(input), offset, count);

        // Advance the offset past any whitespace.
        while (count > 0 && Char.IsWhiteSpace(input[offset]))
        {
            offset++;
            count--;
        }

        // Retreat the count to ignore any padding characters (=).
        while (count > 0 && input[offset + count - 1] == '=')
        {
            count--;
        }

        // Special-case empty input
        if (count <= 0)
        {
            return Array.Empty<byte>();
        }

        var output = new byte[GetArraySizeRequiredToDecode(count)];

        var bitIndex = 0;
        var inputIndex = offset;
        var outputBits = 0;
        var outputIndex = 0;
        while (outputIndex < output.Length)
        {
            var byteIndex = Alphabet.IndexOf(input[inputIndex], StringComparison.OrdinalIgnoreCase);
            if (byteIndex < 0)
            {
                throw new FormatException(String.Format(MalformedInput, inputIndex));
            }

            var bits = Math.Min(BitsPerChar - bitIndex, InputGroupSize - outputBits);
            output[outputIndex] <<= bits;
            output[outputIndex] |= (byte)(byteIndex >> (BitsPerChar - (bitIndex + bits)));

            bitIndex += bits;
            if (bitIndex >= BitsPerChar)
            {
                inputIndex++;
                bitIndex = 0;
            }

            outputBits += bits;
            if (outputBits >= InputGroupSize)
            {
                outputIndex++;
                outputBits = 0;
            }
        }
        return output;
    }

    /// <summary>
    /// Encodes <paramref name="input"/> using base32url encoding.
    /// </summary>
    /// <param name="input">The binary input to encode.</param>
    /// <returns>The base32 form of <paramref name="input"/>.</returns>
    public static string Encode(byte[] input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return Encode(input, offset: 0, count: input.Length);
    }

    /// <summary>
    /// Encodes <paramref name="input"/> using base32url encoding.
    /// </summary>
    /// <param name="input">The binary input to encode.</param>
    /// <param name="offset">The offset into <paramref name="input"/> at which to begin encoding.</param>
    /// <param name="count">The number of bytes from <paramref name="input"/> to encode.</param>
    /// <returns>The base32 form of <paramref name="input"/>.</returns>
    public static string Encode(byte[] input, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(input);
        ValidateParameters(input.Length, nameof(input), offset, count);

        return Encode(input.AsSpan(offset, count));
    }

    /// <summary>
    /// Encodes <paramref name="input"/> using base32url encoding.
    /// </summary>
    /// <param name="input">The binary input to encode.</param>
    /// <param name="offset">The offset into <paramref name="input"/> at which to begin encoding.</param>
    /// <param name="output">
    /// Buffer to receive the base32 form of <paramref name="input"/>. Array must be large enough to
    /// hold <paramref name="outputOffset"/> characters and the full base32-encoded form of
    /// <paramref name="input"/>, including padding characters.
    /// </param>
    /// <param name="outputOffset">
    /// The offset into <paramref name="output"/> at which to begin writing the base32 form of
    /// <paramref name="input"/>.
    /// </param>
    /// <param name="count">The number of <c>byte</c>s from <paramref name="input"/> to encode.</param>
    /// <returns>
    /// The number of characters written to <paramref name="output"/>, less any padding characters.
    /// </returns>
    public static int Encode(byte[] input, int offset, char[] output, int outputOffset, int count)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);
        ValidateParameters(input.Length, nameof(input), offset, count);
        ArgumentOutOfRangeException.ThrowIfNegative(outputOffset);

        var arraySizeRequired = GetArraySizeRequiredToEncode(count);
        if (output.Length - outputOffset < arraySizeRequired)
        {
            throw new ArgumentException(
                String.Format(InvalidCountOffsetOrLength, nameof(count), nameof(outputOffset), nameof(output)),
                nameof(count)
            );
        }

        return Encode(input.AsSpan(offset, count), output.AsSpan(outputOffset));
    }

    /// <summary>
    /// Encodes <paramref name="input"/> using base32url encoding.
    /// </summary>
    /// <param name="input">The binary input to encode.</param>
    /// <returns>The base32 form of <paramref name="input"/>.</returns>
    [SkipLocalsInit]
    public static string Encode(ReadOnlySpan<byte> input)
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

            var numBase32Chars = Encode(input, buffer);
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
    /// Gets the minimum <c>char[]</c> size required for decoding of <paramref name="count"/> characters
    /// with the <see cref="Decode(String, Int32, Int32)"/> method.
    /// </summary>
    /// <param name="count">The number of characters to decode.</param>
    /// <returns>
    /// The minimum <c>char[]</c> size required for decoding  of <paramref name="count"/> characters.
    /// </returns>
    private static int GetArraySizeRequiredToDecode(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return Convert.ToInt32(Math.Floor(count * (float)BitsPerChar / InputGroupSize));
    }

    /// <summary>
    /// Get the minimum output <c>char[]</c> size required for encoding <paramref name="count"/>
    /// <see cref="Byte"/>s with the <see cref="Encode(Byte[], Int32, Char[], Int32, Int32)"/> method.
    /// </summary>
    /// <param name="count">The number of characters to encode.</param>
    /// <returns>
    /// The minimum output <c>char[]</c> size required for encoding <paramref name="count"/> <see cref="Byte"/>s.
    /// </returns>
    private static int GetArraySizeRequiredToEncode(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return Convert.ToInt32(Math.Ceiling(count * (float)InputGroupSize / BitsPerChar));
    }

    private static void ValidateParameters(int bufferLength, string inputName, int offset, int count)
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

#pragma warning disable S107 // SONAR: Methods should not have too many parameters
#pragma warning disable S125 // SONAR: Sections of code should not be commented out
#pragma warning disable S3776 // SONAR: Cognitive Complexity of methods should not be too high
    private static int Encode(ReadOnlySpan<byte> input, Span<char> output)
    {
        if (input.IsEmpty)
        {
            return 0;
        }

        var charsWritten = 0;
        for (int offset = 0; offset < input.Length;)
        {
            int numCharsToOutput = GetNextGroup(input, ref offset, out byte a, out byte b, out byte c, out byte d, out byte e, out byte f, out byte g, out byte h);

            output[charsWritten + 7] = (numCharsToOutput >= 8) ? Alphabet[h] : '=';
            output[charsWritten + 6] = (numCharsToOutput >= 7) ? Alphabet[g] : '=';
            output[charsWritten + 5] = (numCharsToOutput >= 6) ? Alphabet[f] : '=';
            output[charsWritten + 4] = (numCharsToOutput >= 5) ? Alphabet[e] : '=';
            output[charsWritten + 3] = (numCharsToOutput >= 4) ? Alphabet[d] : '=';
            output[charsWritten + 2] = (numCharsToOutput >= 3) ? Alphabet[c] : '=';
            output[charsWritten + 1] = Alphabet[b]; // output[charsWritten + 1] = (numCharsToOutput >= 2) ? Alphabet[b] : '=';
            output[charsWritten] = Alphabet[a]; // output[charsWritten] = (numCharsToOutput >= 1) ? Alphabet[a] : '=';
            charsWritten += 8;
        }

        return charsWritten;
    }

    private static int GetNextGroup(ReadOnlySpan<byte> input, ref int offset, out byte a, out byte b, out byte c, out byte d, out byte e, out byte f, out byte g, out byte h)
    {
        uint b1, b2, b3, b4, b5;

        int retVal;
        switch (input.Length - offset)
        {
            case 1: retVal = 2; break;
            case 2: retVal = 4; break;
            case 3: retVal = 5; break;
            case 4: retVal = 7; break;
            default: retVal = 8; break;
        }

        b1 = input[offset++]; // (offset < input.Length) ? input[offset++] : 0U;
        b2 = (offset < input.Length) ? input[offset++] : 0U;
        b3 = (offset < input.Length) ? input[offset++] : 0U;
        b4 = (offset < input.Length) ? input[offset++] : 0U;
        b5 = (offset < input.Length) ? input[offset++] : 0U;

        a = (byte)(b1 >> 3);
        b = (byte)(((b1 & 0x07) << 2) | (b2 >> 6));
        c = (byte)((b2 >> 1) & 0x1f);
        d = (byte)(((b2 & 0x01) << 4) | (b3 >> 4));
        e = (byte)(((b3 & 0x0f) << 1) | (b4 >> 7));
        f = (byte)((b4 >> 2) & 0x1f);
        g = (byte)(((b4 & 0x3) << 3) | (b5 >> 5));
        h = (byte)(b5 & 0x1f);

        return retVal;
    }
#pragma warning disable S3776
#pragma warning disable S125
#pragma warning disable S107
}

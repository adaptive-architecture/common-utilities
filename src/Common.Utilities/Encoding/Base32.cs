namespace AdaptArch.Common.Utilities.Encoding;

internal class Base32EncodingHelper : Base
{
    private const string MalformedInput = "Malformed input: Input string contains invalid characters at index {0}.";
    private const int BitsPerChar = 5;
    private const int InputGroupSize = 8;
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    protected override byte[] DecodeCore(string input, int offset, int count)
    {
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
    protected override int GetArraySizeRequiredToEncode(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return Convert.ToInt32(Math.Ceiling(count * (float)InputGroupSize / BitsPerChar));
    }

    // SONAR: Methods should not have too many parameters
    // SONAR: Sections of code should not be commented out
    // SONAR: Cognitive Complexity of methods should not be too high
#pragma warning disable S107, S125, S3776
    protected override int EncodeCore(ReadOnlySpan<byte> input, Span<char> output)
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
#pragma warning restore S107, S125, S3776

    private static int GetArraySizeRequiredToDecode(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        return Convert.ToInt32(Math.Floor(count * (float)BitsPerChar / InputGroupSize));
    }
}

/// <summary>
/// Contains utility APIs to assist with encoding and decoding operations using <see href="https://datatracker.ietf.org/doc/html/rfc4648#section-6">Base 32</see>.
/// </summary>
/// /// <remarks>
/// Inspired by <see href="https://github.com/dotnet/aspnetcore/blob/c096dbbbe652f03be926502d790eb499682eea13/src/Identity/Extensions.Core/src/Base32.cs">AspNetCore.Identity</see>.
/// </remarks>
public static class Base32
{
    private static readonly Base32EncodingHelper s_helper = new();

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
        => s_helper.Decode(input);

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
        => s_helper.Decode(input, offset, count);

    /// <summary>
    /// Encodes <paramref name="input"/> using base32url encoding.
    /// </summary>
    /// <param name="input">The binary input to encode.</param>
    /// <returns>The base32 form of <paramref name="input"/>.</returns>
    public static string Encode(byte[] input)
        => s_helper.Encode(input);

    /// <summary>
    /// Encodes <paramref name="input"/> using base32url encoding.
    /// </summary>
    /// <param name="input">The binary input to encode.</param>
    /// <param name="offset">The offset into <paramref name="input"/> at which to begin encoding.</param>
    /// <param name="count">The number of bytes from <paramref name="input"/> to encode.</param>
    /// <returns>The base32 form of <paramref name="input"/>.</returns>
    public static string Encode(byte[] input, int offset, int count)
        => s_helper.Encode(input, offset, count);

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
        => s_helper.Encode(input, offset, output, outputOffset, count);

    /// <summary>
    /// Encodes <paramref name="input"/> using base32url encoding.
    /// </summary>
    /// <param name="input">The binary input to encode.</param>
    /// <returns>The base32 form of <paramref name="input"/>.</returns>
    public static string Encode(ReadOnlySpan<byte> input)
        => s_helper.EncodeLocal(input);
}

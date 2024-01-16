namespace AdaptArch.Common.Utilities.Encoding;

internal class Base64UrlEncodingHelper : Base
{
    private const string MalformedInput = "Malformed input: {0} is an invalid input length.";

    internal static byte[] Decode(string input, int offset, char[] buffer, int bufferOffset, int count)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(buffer);

        ValidateParameters(input.Length, nameof(input), offset, count);
        ArgumentOutOfRangeException.ThrowIfNegative(bufferOffset);

        if (count == 0)
        {
            return Array.Empty<byte>();
        }

        // Assumption: input is base64url encoded without padding and contains no whitespace.

        var paddingCharsToAdd = GetNumBase64PaddingCharsToAddForDecode(count);
        var arraySizeRequired = checked(count + paddingCharsToAdd);

        ValidateBufferLengths(buffer, bufferOffset, arraySizeRequired);

        // Copy input into buffer, fixing up '-' -> '+' and '_' -> '/'.
        var i = bufferOffset;
        for (var j = offset; i - bufferOffset < count; i++, j++)
        {
            var ch = input[j];
            if (ch == '-')
            {
                buffer[i] = '+';
            }
            else if (ch == '_')
            {
                buffer[i] = '/';
            }
            else
            {
                buffer[i] = ch;
            }
        }

        // Add the padding characters back.
        for (; paddingCharsToAdd > 0; i++, paddingCharsToAdd--)
        {
            buffer[i] = '=';
        }

        // Decode.
        // If the caller provided invalid base64 chars, they'll be caught here.
        return Convert.FromBase64CharArray(buffer, bufferOffset, arraySizeRequired);
    }

    internal static int GetArraySizeRequiredToDecode(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
        {
            return 0;
        }

        var numPaddingCharsToAdd = GetNumBase64PaddingCharsToAddForDecode(count);

        return checked(count + numPaddingCharsToAdd);
    }

    internal static int GetSizeRequiredToEncode(int count)
    {
        var numWholeOrPartialInputBlocks = checked(count + 2) / 3;
        return checked(numWholeOrPartialInputBlocks * 4);
    }

    protected override byte[] DecodeCore(string input, int offset, int count)
    {
        var buffer = new char[GetArraySizeRequiredToDecode(count)];
        return Decode(input, offset, buffer, bufferOffset: 0, count: count);
    }
    protected override int EncodeCore(ReadOnlySpan<byte> input, Span<char> output)
    {
        if (input.IsEmpty)
        {
            return 0;
        }

        // Use base64url encoding with no padding characters. See RFC 4648, Sec. 5.

        Convert.TryToBase64Chars(input, output, out var charsWritten);

        // Fix up '+' -> '-' and '/' -> '_'. Drop padding characters.
        for (var i = 0; i < charsWritten; i++)
        {
            var ch = output[i];
            if (ch == '+')
            {
                output[i] = '-';
            }
            else if (ch == '/')
            {
                output[i] = '_';
            }
            else if (ch == '=')
            {
                // We've reached a padding character; truncate the remainder.
                return i;
            }
        }

        return charsWritten;
    }
    protected override int GetArraySizeRequiredToEncode(int count)
        => GetSizeRequiredToEncode(count);

    private static int GetNumBase64PaddingCharsToAddForDecode(int inputLength)
    {
        switch (inputLength % 4)
        {
            case 0:
                return 0;
            case 2:
                return 2;
            case 3:
                return 1;
            default:
                throw new FormatException(String.Format(MalformedInput, inputLength));
        }
    }
}

/// <summary>
/// Contains utility APIs to assist with encoding and decoding operations using <see href="https://datatracker.ietf.org/doc/html/rfc4648#section-5">URL and Filename Safe Alphabet</see>.
/// </summary>
/// <remarks>
/// Inspired by <see href="https://github.com/dotnet/aspnetcore/blob/main/src/Shared/WebEncoders/WebEncoders.cs">AspNetCore.WebEncoders</see>.
/// </remarks>
public static class Base64Url
{
    private static readonly Base64UrlEncodingHelper s_helper = new();

    /// <summary>
    /// Decodes a base64url-encoded string.
    /// </summary>
    /// <param name="input">The base64url-encoded input to decode.</param>
    /// <returns>The base64url-decoded form of the input.</returns>
    /// <remarks>
    /// The input must not contain any whitespace or padding characters.
    /// Throws <see cref="FormatException"/> if the input is malformed.
    /// </remarks>
    public static byte[] Decode(string input)
        => s_helper.Decode(input);

    /// <summary>
    /// Decodes a base64url-encoded substring of a given string.
    /// </summary>
    /// <param name="input">A string containing the base64url-encoded input to decode.</param>
    /// <param name="offset">The position in <paramref name="input"/> at which decoding should begin.</param>
    /// <param name="count">The number of characters in <paramref name="input"/> to decode.</param>
    /// <returns>The base64url-decoded form of the input.</returns>
    /// <remarks>
    /// The input must not contain any whitespace or padding characters.
    /// Throws <see cref="FormatException"/> if the input is malformed.
    /// </remarks>
    public static byte[] Decode(string input, int offset, int count)
        => s_helper.Decode(input, offset, count);

    /// <summary>
    /// Decodes a base64url-encoded <paramref name="input"/> into a <c>byte[]</c>.
    /// </summary>
    /// <param name="input">A string containing the base64url-encoded input to decode.</param>
    /// <param name="offset">The position in <paramref name="input"/> at which decoding should begin.</param>
    /// <param name="buffer">
    /// Scratch buffer to hold the <see cref="Char"/>s to decode. Array must be large enough to hold
    /// <paramref name="bufferOffset"/> and <paramref name="count"/> characters as well as Base64 padding
    /// characters. Content is not preserved.
    /// </param>
    /// <param name="bufferOffset">
    /// The offset into <paramref name="buffer"/> at which to begin writing the <see cref="Char"/>s to decode.
    /// </param>
    /// <param name="count">The number of characters in <paramref name="input"/> to decode.</param>
    /// <returns>The base64url-decoded form of the <paramref name="input"/>.</returns>
    /// <remarks>
    /// The input must not contain any whitespace or padding characters.
    /// Throws <see cref="FormatException"/> if the input is malformed.
    /// </remarks>
    public static byte[] Decode(string input, int offset, char[] buffer, int bufferOffset, int count)
        => Base64UrlEncodingHelper.Decode(input, offset, buffer, bufferOffset, count);

    /// <summary>
    /// Gets the minimum <c>char[]</c> size required for decoding of <paramref name="count"/> characters
    /// with the <see cref="Decode(String, Int32, Char[], Int32, Int32)"/> method.
    /// </summary>
    /// <param name="count">The number of characters to decode.</param>
    /// <returns>
    /// The minimum <c>char[]</c> size required for decoding  of <paramref name="count"/> characters.
    /// </returns>
    public static int GetArraySizeRequiredToDecode(int count)
        => Base64UrlEncodingHelper.GetArraySizeRequiredToDecode(count);

    /// <summary>
    /// Get the minimum output <c>char[]</c> size required for encoding <paramref name="count"/>
    /// <see cref="Byte"/>s with the <see cref="Encode(Byte[], Int32, Char[], Int32, Int32)"/> method.
    /// </summary>
    /// <param name="count">The number of characters to encode.</param>
    /// <returns>
    /// The minimum output <c>char[]</c> size required for encoding <paramref name="count"/> <see cref="Byte"/>s.
    /// </returns>
    public static int GetArraySizeRequiredToEncode(int count)
        => Base64UrlEncodingHelper.GetSizeRequiredToEncode(count);

    /// <summary>
    /// Encodes <paramref name="input"/> using base64url encoding.
    /// </summary>
    /// <param name="input">The binary input to encode.</param>
    /// <returns>The base64url-encoded form of <paramref name="input"/>.</returns>
    public static string Encode(byte[] input)
        => s_helper.Encode(input);

    /// <summary>
    /// Encodes <paramref name="input"/> using base64url encoding.
    /// </summary>
    /// <param name="input">The binary input to encode.</param>
    /// <param name="offset">The offset into <paramref name="input"/> at which to begin encoding.</param>
    /// <param name="count">The number of bytes from <paramref name="input"/> to encode.</param>
    /// <returns>The base64url-encoded form of <paramref name="input"/>.</returns>
    public static string Encode(byte[] input, int offset, int count)
        => s_helper.Encode(input, offset, count);

    /// <summary>
    /// Encodes <paramref name="input"/> using base64url encoding.
    /// </summary>
    /// <param name="input">The binary input to encode.</param>
    /// <param name="offset">The offset into <paramref name="input"/> at which to begin encoding.</param>
    /// <param name="output">
    /// Buffer to receive the base64url-encoded form of <paramref name="input"/>. Array must be large enough to
    /// hold <paramref name="outputOffset"/> characters and the full base64-encoded form of
    /// <paramref name="input"/>, including padding characters.
    /// </param>
    /// <param name="outputOffset">
    /// The offset into <paramref name="output"/> at which to begin writing the base64url-encoded form of
    /// <paramref name="input"/>.
    /// </param>
    /// <param name="count">The number of <c>byte</c>s from <paramref name="input"/> to encode.</param>
    /// <returns>
    /// The number of characters written to <paramref name="output"/>, less any padding characters.
    /// </returns>
    public static int Encode(byte[] input, int offset, char[] output, int outputOffset, int count)
        => s_helper.Encode(input, offset, output, outputOffset, count);

    /// <summary>
    /// Encodes <paramref name="input"/> using base64url encoding.
    /// </summary>
    /// <param name="input">The binary input to encode.</param>
    /// <returns>The base64url-encoded form of <paramref name="input"/>.</returns>
    public static string Encode(ReadOnlySpan<byte> input)
        => s_helper.EncodeLocal(input);
}

using BclBase64Url = System.Buffers.Text.Base64Url;
namespace AdaptArch.Common.Utilities.Encoding;

/// <summary>
/// Contains utility APIs to assist with encoding and decoding operations using <see href="https://datatracker.ietf.org/doc/html/rfc4648#section-5">URL and Filename Safe Alphabet</see>.
/// </summary>
/// <remarks>
/// Inspired by <see href="https://github.com/dotnet/aspnetcore/blob/main/src/Shared/WebEncoders/WebEncoders.cs">AspNetCore.WebEncoders</see>.
/// </remarks>
public static class Base64Url
{
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
        => BclBase64Url.DecodeFromChars(input);

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
        => BclBase64Url.DecodeFromChars(input.AsSpan(offset, count));

    /// <summary>
    /// Encodes <paramref name="input"/> using base64url encoding.
    /// </summary>
    /// <param name="input">The binary input to encode.</param>
    /// <returns>The base64url-encoded form of <paramref name="input"/>.</returns>
    public static string Encode(byte[] input)
        => BclBase64Url.EncodeToString(input);

    /// <summary>
    /// Encodes <paramref name="input"/> using base64url encoding.
    /// </summary>
    /// <param name="input">The binary input to encode.</param>
    /// <param name="offset">The offset into <paramref name="input"/> at which to begin encoding.</param>
    /// <param name="count">The number of bytes from <paramref name="input"/> to encode.</param>
    /// <returns>The base64url-encoded form of <paramref name="input"/>.</returns>
    public static string Encode(byte[] input, int offset, int count)
        => BclBase64Url.EncodeToString(input.AsSpan(offset, count));

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
        => BclBase64Url.EncodeToChars(input.AsSpan(offset, count), output.AsSpan(outputOffset));

    /// <summary>
    /// Encodes <paramref name="input"/> using base64url encoding.
    /// </summary>
    /// <param name="input">The binary input to encode.</param>
    /// <returns>The base64url-encoded form of <paramref name="input"/>.</returns>
    public static string Encode(ReadOnlySpan<byte> input)
        => BclBase64Url.EncodeToString(input);
}

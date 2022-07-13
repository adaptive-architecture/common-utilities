namespace AdaptArch.Common.Utilities.Encoding
{
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
        public static byte[] Decode(string input)
        {
            return Array.Empty<byte>();
        }

    }
}

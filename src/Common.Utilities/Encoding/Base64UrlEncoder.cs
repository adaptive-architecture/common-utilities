namespace AdaptArch.Common.Utilities.Encoding;

/// <summary>
/// An wrapper arouynd the <see cref="Base64Url"/> API implementing the <see cref="IEncoder"/> contract.
/// </summary>
public class Base64UrlEncoder : IEncoder
{
    /// <inheritdoc/>
    public byte[] Decode(string input) => Base64Url.Decode(input);
    /// <inheritdoc/>
    public string Encode(byte[] input) => Base64Url.Encode(input);
}

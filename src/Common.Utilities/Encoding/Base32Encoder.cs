namespace AdaptArch.Common.Utilities.Encoding;

/// <summary>
/// An wrapper around the <see cref="Base32"/> API implementing the <see cref="IEncoder"/> contract.
/// </summary>
public class Base32Encoder : IEncoder
{
    /// <inheritdoc/>
    public byte[] Decode(string input) => Base32.Decode(input);
    /// <inheritdoc/>
    public string Encode(byte[] input) => Base32.Encode(input);
}

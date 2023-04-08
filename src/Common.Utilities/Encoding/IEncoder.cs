namespace AdaptArch.Common.Utilities.Encoding;

/// <summary>
/// API Contract to encode and decode information.
/// </summary>
public interface IEncoder
{
    /// <summary>
    /// Decode the information encoded in the input.
    /// </summary>
    /// <param name="input">The encoded input to decode.</param>
    /// <returns>The decoded information.</returns>
    public byte[] Decode(string input);

    /// <summary>
    /// Encode the information received in the input.
    /// </summary>
    /// <param name="input">The information to encode.</param>
    /// <returns>The encoded information.</returns>
    public string Encode(byte[] input);
}

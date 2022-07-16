namespace AdaptArch.Common.Utilities.Encoding;

/// <summary>
/// API Contract to encode and decode infromation.
/// </summary>
public interface IEncoder
{
    /// <summary>
    /// Decode the infromation encoded in the input.
    /// </summary>
    /// <param name="input">The encoded input to decode.</param>
    /// <returns>The decoded information.</returns>
    public byte[] Decode(string input);

    /// <summary>
    /// Encode the infromation recieved in the input.
    /// </summary>
    /// <param name="input">The infromation to encode.</param>
    /// <returns>The encoded information.</returns>
    public string Encode(byte[] input);
}

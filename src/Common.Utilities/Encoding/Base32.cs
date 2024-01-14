namespace AdaptArch.Common.Utilities.Encoding;

/// <summary>
/// Contains utility APIs to assist with encoding and decoding operations using <see href="https://datatracker.ietf.org/doc/html/rfc4648#section-6">Base 32</see>.
/// </summary>
public static class Base32
{
    public static byte[] Decode(string input) { }
    public static byte[] Decode(string input, int offset, int count) { }
    public static byte[] Decode(string input, int offset, char[] buffer, int bufferOffset, int count) { }
    public static string Encode(byte[] input) { }
    public static string Encode(byte[] input, int offset, int count) { }
    public static int Encode(byte[] input, int offset, char[] output, int outputOffset, int count) { }

}

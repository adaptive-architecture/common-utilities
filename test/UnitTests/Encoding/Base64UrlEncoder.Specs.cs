using AdaptArch.Common.Utilities.Encoding;

namespace AdaptArch.UnitTests.Encoding;

public class Base64UrlEncoderSpecs
{


    [Fact]
    public void DataOfVariousLengthRoundTripCorrectly()
    {
        IEncoder encoder = new Base64UrlEncoder();
        for (int length = 0; length < 256; ++length)
        {
            var data = new byte[length];
            for (int index = 0; index < length; ++index)
            {
                data[index] = (byte)(5 + length + (index * 23));
            }
            string text = encoder.Encode(data);
            byte[] result = encoder.Decode(text);

            for (int index = 0; index < length; ++index)
            {
                Assert.Equal(data[index], result[index]);
            }
        }
    }
}

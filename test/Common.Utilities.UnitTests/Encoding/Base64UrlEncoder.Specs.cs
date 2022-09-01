using AdaptArch.Common.Utilities.Encoding;

namespace AdaptArch.Common.Utilities.UnitTests.Encoding;

public class Base64UrlEncoderSpecs
{
    [Fact]
    public void DataOfVariousLengthRoundTripCorrectly()
    {
        IEncoder encoder = new Base64UrlEncoder();
        for (var length = 0; length < 256; ++length)
        {
            var data = new byte[length];
            for (var index = 0; index < length; ++index)
            {
                data[index] = (byte)(5 + length + (index * 23));
            }
            var text = encoder.Encode(data);
            var result = encoder.Decode(text);

            for (var index = 0; index < length; ++index)
            {
                Assert.Equal(data[index], result[index]);
            }
        }
    }
}

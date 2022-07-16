using AdaptArch.Common.Utilities.Encoding;

namespace AdaptArch.UnitTests.Encoding;

public class Base64UrlSpecs
{
    [Theory]
    [InlineData("", 1, 0)]
    [InlineData("", 0, 1)]
    [InlineData("0123456789", 9, 2)]
    [InlineData("0123456789", Int32.MaxValue, 2)]
    [InlineData("0123456789", 9, -1)]
    [InlineData("0123456789", -1, -1)]
    public void Base64UrlDecode_BadOffsets(string input, int offset, int count)
    {
        Assert.ThrowsAny<ArgumentException>(() =>
        {
            var retVal = Base64Url.Decode(input, offset, count);
        });
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(10, 9, 2)]
    [InlineData(10, Int32.MaxValue, 2)]
    [InlineData(10, 9, -1)]
    public void Base64UrlEncode_BadOffsets(int inputLength, int offset, int count)
    {
        byte[] input = new byte[inputLength];

        Assert.ThrowsAny<ArgumentException>(() =>
        {
            var retVal = Base64Url.Encode(input, offset, count);
        });
    }

    [Fact]
    public void DataOfVariousLengthRoundTripCorrectly()
    {
        for (int length = 0; length < 256; ++length)
        {
            var data = new byte[length];
            for (int index = 0; index < length; ++index)
            {
                data[index] = (byte)(5 + length + (index * 23));
            }
            string text = Base64Url.Encode(data);
            byte[] result = Base64Url.Decode(text);

            for (int index = 0; index < length; ++index)
            {
                Assert.Equal(data[index], result[index]);
            }
        }
    }


    [Theory]
    [InlineData(0, 0)]
    [InlineData(2, 4)]
    [InlineData(3, 4)]
    [InlineData(4, 4)]
    public void GetArraySizeRequiredToDecode_Return_Correct_Data(int count, int result)
    {
        Assert.Equal(result, Base64Url.GetArraySizeRequiredToDecode(count));
    }

    [Fact]
    public void GetArraySizeRequiredToDecode_Throws_Format_Exception()
    {
        Assert.Throws<FormatException>(() =>
        {
            _ = Base64Url.GetArraySizeRequiredToDecode(1);
        });
    }

    [Fact]
    public void GetArraySizeRequiredToDecode_Throws_ArgumentOutOfRange_Exception()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            _ = Base64Url.GetArraySizeRequiredToDecode(-1);
        });
    }
}

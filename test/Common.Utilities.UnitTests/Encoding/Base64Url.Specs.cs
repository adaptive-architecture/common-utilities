using AdaptArch.Common.Utilities.Encoding;
// ReSharper disable AssignNullToNotNullAttribute

namespace AdaptArch.Common.Utilities.UnitTests.Encoding;

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
        Assert.ThrowsAny<ArgumentException>(() => _ = Base64Url.Decode(input, offset, count));
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(10, 9, 2)]
    [InlineData(10, Int32.MaxValue, 2)]
    [InlineData(10, 9, -1)]
    public void Base64UrlEncode_BadOffsets(int inputLength, int offset, int count)
    {
        var input = new byte[inputLength];

        Assert.ThrowsAny<ArgumentException>(() => _ = Base64Url.Encode(input, offset, count));
    }

    [Fact]
    public void DataOfVariousLengthRoundTripCorrectly()
    {
        for (var length = 0; length < 256; ++length)
        {
            var data = new byte[length];
            for (var index = 0; index < length; ++index)
            {
                data[index] = (byte)(5 + length + (index * 23));
            }
            var text = Base64Url.Encode(data);
            var result = Base64Url.Decode(text);

            for (var index = 0; index < length; ++index)
            {
                Assert.Equal(data[index], result[index]);
            }
        }
    }

    [Fact]
    public void Decode_Empty_ReturnsEmpty()
    {
        Assert.Empty(Base64Url.Decode("0123456789", 0, Array.Empty<char>(), 0, 0));
    }

    [Fact]
    public void Decode_Throws_ArgumentNull_Exception_1()
    {
        Assert.Throws<ArgumentNullException>(() => _ = Base64Url.Decode(null));
    }

    [Fact]
    public void Decode_Throws_ArgumentNull_Exception_2()
    {
        Assert.Throws<ArgumentNullException>(() => _ = Base64Url.Decode(null, 0, 0));
    }

    [Fact]
    public void Decode_Throws_ArgumentNull_Exception_3()
    {
        Assert.Throws<ArgumentNullException>(() => _ = Base64Url.Decode(null, 0, Array.Empty<char>(), 0, 0));
    }

    [Fact]
    public void Decode_Throws_ArgumentNull_Exception_4()
    {
        Assert.Throws<ArgumentNullException>(() => _ = Base64Url.Decode(String.Empty, 0, null, 0, 0));
    }

    [Fact]
    public void Decode_Throws_ArgumentOutOfRange_Exception_4()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Base64Url.Decode(String.Empty, 0, Array.Empty<char>(), -1, 0));
    }

    [Fact]
    public void Decode_Throws_Argument_Exception_4()
    {
        Assert.Throws<ArgumentException>(() => _ = Base64Url.Decode("0123456789", 0, Array.Empty<char>(), 2, 2));
    }

    [Fact]
    public void Encode_Empty_ReturnsEmpty()
    {
        Assert.Equal(0, Base64Url.Encode(Array.Empty<byte>(), 0, Array.Empty<char>(), 0, 0));
    }

    [Fact]
    public void Encode_Throws_ArgumentNull_Exception_1()
    {
        Assert.Throws<ArgumentNullException>(() => _ = Base64Url.Encode(null));
    }

    [Fact]
    public void Encode_Throws_ArgumentNull_Exception_2()
    {
        Assert.Throws<ArgumentNullException>(() => _ = Base64Url.Encode(null, 0, 0));
    }

    [Fact]
    public void Encode_Throws_ArgumentNull_Exception_3()
    {
        Assert.Throws<ArgumentNullException>(() => _ = Base64Url.Encode(null, 0, Array.Empty<char>(), 0, 0));
    }

    [Fact]
    public void Encode_Throws_ArgumentNull_Exception_4()
    {
        Assert.Throws<ArgumentNullException>(() => _ = Base64Url.Encode(Array.Empty<byte>(), 0, null, 0, 0));
    }

    [Fact]
    public void Encode_Throws_ArgumentOutOfRange_Exception_4()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Base64Url.Encode(Array.Empty<byte>(), 0, Array.Empty<char>(), -1, 0));
    }

    [Fact]
    public void Encode_Throws_Argument_Exception_4()
    {
        Assert.Throws<ArgumentException>(() => _ = Base64Url.Encode(Array.Empty<byte>(), 0, Array.Empty<char>(), 2, 0));
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
        Assert.Throws<ArgumentOutOfRangeException>(() => _ = Base64Url.GetArraySizeRequiredToDecode(-1));
    }
}

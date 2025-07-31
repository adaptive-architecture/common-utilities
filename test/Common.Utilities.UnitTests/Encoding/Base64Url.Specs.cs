using AdaptArch.Common.Utilities.Encoding;
// ReSharper disable AssignNullToNotNullAttribute

namespace AdaptArch.Common.Utilities.UnitTests.Encoding;

public class Base64UrlSpecs
{
    private static readonly byte[] _testData = System.Text.Encoding.UTF8.GetBytes("lorem ipsum");
    private const string _testDataEncoded = "bG9yZW0gaXBzdW0";

    [Fact]
    public void Simple_Encode_Test()
    {
        Assert.Equal(_testDataEncoded, Base64Url.Encode(_testData));
    }

    [Fact]
    public void Simple_Encode_Span_Test()
    {
        Assert.Equal(_testDataEncoded, Base64Url.Encode(_testData.AsSpan()));
    }

    [Theory]
    [InlineData("", 1, 0)]
    [InlineData("", 0, 1)]
    [InlineData("0123456789", 9, 2)]
    [InlineData("0123456789", Int32.MaxValue, 2)]
    [InlineData("0123456789", 9, -1)]
    [InlineData("0123456789", -1, -1)]
    public void Base64UrlDecode_BadOffsets(string input, int offset, int count)
    {
        _ = Assert.ThrowsAny<ArgumentException>(() => _ = Base64Url.Decode(input, offset, count));
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

        _ = Assert.ThrowsAny<ArgumentException>(() => _ = Base64Url.Encode(input, offset, count));
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
    public void Encode_Empty_ReturnsEmpty()
    {
        Assert.Equal(0, Base64Url.Encode([], 0, [], 0, 0));
    }

    [Fact]
    public void Encode_Throws_ArgumentOutOfRange_Exception_4()
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => _ = Base64Url.Encode([], 0, [], -1, 0));
    }
}

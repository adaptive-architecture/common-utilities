using System.Security.Cryptography;
using AdaptArch.Common.Utilities.Encoding;
// ReSharper disable AssignNullToNotNullAttribute

namespace AdaptArch.Common.Utilities.UnitTests.Encoding;

public class Base32Specs
{
    private static readonly RandomNumberGenerator _rng = RandomNumberGenerator.Create();
    private static readonly byte[] _testData = System.Text.Encoding.UTF8.GetBytes("lorem ipsum");
    private const string _testDataEncoded = "NRXXEZLNEBUXA43VNU======";

    [Fact]
    public void ConversionTest()
    {
        var data = new byte[] { 1, 2, 3, 4, 5, 6 };
        Assert.Equal(data, Base32.Decode(Base32.Encode(data)));

        int length;
        do
        {
            length = GetRandomByteArray(1)[0];
        } while (length % 5 == 0);
        data = GetRandomByteArray(length);
        Assert.Equal(data, Base32.Decode(Base32.Encode(data)));

        length = GetRandomByteArray(1)[0] * 5;
        data = GetRandomByteArray(length);
        Assert.Equal(data, Base32.Decode(Base32.Encode(data)));
    }

    [Fact]
    public void Simple_Encode_Test()
    {
        Assert.Equal(_testDataEncoded, Base32.Encode(_testData));
    }

    [Fact]
    public void Simple_Encode_Span_Test()
    {
        Assert.Equal(_testDataEncoded, Base32.Encode(_testData.AsSpan()));
    }

    [Theory]
    [InlineData("", 1, 0)]
    [InlineData("", 0, 1)]
    [InlineData("0123456789", 9, 2)]
    [InlineData("0123456789", Int32.MaxValue, 2)]
    [InlineData("0123456789", 9, -1)]
    [InlineData("0123456789", -1, -1)]
    public void Base32Decode_BadOffsets(string input, int offset, int count)
    {
        _ = Assert.ThrowsAny<ArgumentException>(() => _ = Base32.Decode(input, offset, count));
    }

    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(0, 0, 1)]
    [InlineData(10, 9, 2)]
    [InlineData(10, Int32.MaxValue, 2)]
    [InlineData(10, 9, -1)]
    public void Base32Encode_BadOffsets(int inputLength, int offset, int count)
    {
        var input = new byte[inputLength];

        _ = Assert.ThrowsAny<ArgumentException>(() => _ = Base32.Encode(input, offset, count));
    }

    [Fact]
    public void DataOfVariousLengthRoundTripCorrectly()
    {
        for (var length = 0; length < 256; ++length)
        {
            var data = new byte[length];
            Array.Fill<byte>(data, 0);
            var text = Base32.Encode(data);
            var result = Base32.Decode(text);

            for (var index = 0; index < length; ++index)
            {
                Assert.Equal(data[index], result[index]);
            }
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Decode_Empty_ReturnsEmpty(string encoded)
    {
        Assert.Empty(Base32.Decode(encoded, 0, encoded.Length));
    }

    [Fact]
    public void Decode_Throws_ArgumentNull_Exception_1()
    {
        _ = Assert.Throws<ArgumentNullException>(() => _ = Base32.Decode(null));
    }

    [Fact]
    public void Decode_Throws_ArgumentNull_Exception_2()
    {
        _ = Assert.Throws<ArgumentNullException>(() => _ = Base32.Decode(null, 0, 0));
    }

    [Fact]
    public void Encode_Empty_ReturnsEmpty()
    {
        Assert.Equal(0, Base32.Encode(Array.Empty<byte>(), 0, Array.Empty<char>(), 0, 0));
    }

    [Fact]
    public void Encode_Throws_ArgumentNull_Exception_1()
    {
        _ = Assert.Throws<ArgumentNullException>(() => _ = Base32.Encode(null));
    }

    [Fact]
    public void Encode_Throws_ArgumentNull_Exception_2()
    {
        _ = Assert.Throws<ArgumentNullException>(() => _ = Base32.Encode(null, 0, 0));
    }

    [Fact]
    public void Encode_Throws_ArgumentNull_Exception_3()
    {
        _ = Assert.Throws<ArgumentNullException>(() => _ = Base32.Encode(null, 0, Array.Empty<char>(), 0, 0));
    }

    [Fact]
    public void Encode_Throws_ArgumentNull_Exception_4()
    {
        _ = Assert.Throws<ArgumentNullException>(() => _ = Base32.Encode(Array.Empty<byte>(), 0, null, 0, 0));
    }

    [Fact]
    public void Encode_Throws_ArgumentOutOfRange_Exception_4()
    {
        _ = Assert.Throws<ArgumentOutOfRangeException>(() => _ = Base32.Encode(Array.Empty<byte>(), 0, Array.Empty<char>(), -1, 0));
    }

    [Fact]
    public void Encode_Throws_Argument_Exception_4()
    {
        _ = Assert.Throws<ArgumentException>(() => _ = Base32.Encode(Array.Empty<byte>(), 0, Array.Empty<char>(), 2, 0));
    }

    [Fact]
    public void Decode_Throws_Format_Exception()
    {
        _ = Assert.Throws<FormatException>(() => _ = Base32.Decode("ORSXG1A="));
    }

    private static byte[] GetRandomByteArray(int length)
    {
        byte[] bytes = new byte[length];
        _rng.GetBytes(bytes);
        return bytes;
    }
}

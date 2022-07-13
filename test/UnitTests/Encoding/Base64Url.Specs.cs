using AdaptArch.Common.Utilities.Encoding;

namespace AdaptArch.UnitTests.Encoding
{
    public class Base64UrlSpecs
    {
        [Fact]
        public void Test1()
        {
            Assert.NotNull(Base64Url.Decode(String.Empty));
        }
    }
}

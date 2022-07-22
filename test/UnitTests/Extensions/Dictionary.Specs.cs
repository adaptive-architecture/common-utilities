using AdaptArch.Common.Utilities.Extensions;

namespace AdaptArch.UnitTests.Extensions;

public class DictionarySpecs
{
    private class TestClass
    {
        public string Id { get; set; }
    }

    [Fact]
    public void TryGetValueOrDefault_Should_Return_The_Item_If_It_Exists()
    {
        var dictionary = new Dictionary<int, TestClass>
        {
            {1, new TestClass { Id = "1" }}
        };

        Assert.True(dictionary.TryGetValueOrDefault(1, _ => null, out var v));
        Assert.Same(dictionary[1], v);
    }

    [Fact]
    public void TryGetValueOrDefault_Should_Return_Default_If_Missing()
    {
        var dictionary = new Dictionary<int, TestClass>();
        var defaultValue = new TestClass {Id = "1"};
        Assert.False(dictionary.TryGetValueOrDefault(1, _ => defaultValue, out var v));
        Assert.Same(defaultValue, v);
    }

    [Fact]
    public void TryGetValueOrDefault_Should_Return_Default_If_Null()
    {
        var dictionary = new Dictionary<int, TestClass> {{1, null}};
        var defaultValue = new TestClass { Id = "1" };
        Assert.False(dictionary.TryGetValueOrDefault(1, _ => defaultValue, out var v));
        Assert.Same(defaultValue, v);
    }
}

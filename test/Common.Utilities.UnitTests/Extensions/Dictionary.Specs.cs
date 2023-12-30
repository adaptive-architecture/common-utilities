using AdaptArch.Common.Utilities.Extensions;

namespace AdaptArch.Common.Utilities.UnitTests.Extensions;

public class DictionarySpecs
{
    private class TestClass
    {
        public string Id { get; init; }
    }

    [Fact]
    public void TryGetValueOrDefault_Should_Return_The_Item_If_It_Exists()
    {
        var dictionary = new Dictionary<int, TestClass>
        {
            {1, new TestClass { Id = "1" }}
        };

        Assert.True(dictionary.TryGetValueOrDefault(1, _ => null, out var v));
        Assert.Equal(dictionary[1].Id, v.Id);
        Assert.Same(dictionary[1], v);
    }

    [Fact]
    public void TryGetValueOrDefault_Should_Return_Default_If_Missing()
    {
        var dictionary = new Dictionary<int, TestClass>();
        var defaultValue = new TestClass { Id = "1" };
        Assert.False(dictionary.TryGetValueOrDefault(1, _ => defaultValue, out var v));
        Assert.Same(defaultValue, v);
    }

    [Fact]
    public void TryGetValueOrDefault_Should_Return_Default_If_Null()
    {
        var dictionary = new Dictionary<int, TestClass> { { 1, null } };
        var defaultValue = new TestClass { Id = "1" };
        Assert.False(dictionary.TryGetValueOrDefault(1, _ => defaultValue, out var v));
        Assert.Same(defaultValue, v);
    }

    [Fact]
    public void GetValueOrDefault_Should_Return_The_Item_If_It_Exists()
    {
        var dictionary = new Dictionary<int, TestClass>
        {
            {1, new TestClass { Id = "1" }}
        };

        Assert.Same(dictionary[1], dictionary.GetValueOrDefault(1, _ => null));
    }

    [Fact]
    public void GetValueOrDefault_Should_Return_Default_If_Missing()
    {
        var dictionary = new Dictionary<int, TestClass>();
        var defaultValue = new TestClass { Id = "1" };
        Assert.Same(defaultValue, dictionary.GetValueOrDefault(1, _ => defaultValue));
    }

    [Fact]
    public void GetValueOrDefault_Should_Return_Default_If_Null()
    {
        var dictionary = new Dictionary<int, TestClass> { { 1, null } };
        var defaultValue = new TestClass { Id = "1" };
        Assert.Same(defaultValue, dictionary.GetValueOrDefault(1, _ => defaultValue));
    }

    [Fact]
    public void TryGetValueOrDefault_Should_Return_The_Item_If_It_Exists_Without_Setting_The_Value()
    {
        var dictionary = new Dictionary<int, TestClass>
        {
            {1, new TestClass { Id = "1" }}
        };

        Assert.True(dictionary.TryGetValueOrDefault(1, _ => null, false, out var v));
        Assert.Same(dictionary[1], v);
    }

    [Fact]
    public void TryGetValueOrDefault_Should_Return_Default_If_Missing_Without_Setting_The_Value()
    {
        var dictionary = new Dictionary<int, TestClass>();
        var defaultValue = new TestClass { Id = "1" };
        Assert.False(dictionary.TryGetValueOrDefault(1, _ => defaultValue, false, out var v));
        Assert.Same(defaultValue, v);
    }

    [Fact]
    public void TryGetValueOrDefault_Should_Return_Default_If_Null_Without_Setting_The_Value()
    {
        var dictionary = new Dictionary<int, TestClass> { { 1, null } };
        var defaultValue = new TestClass { Id = "1" };
        Assert.False(dictionary.TryGetValueOrDefault(1, _ => defaultValue, false, out var v));
        Assert.Same(defaultValue, v);
    }

    [Fact]
    public void GetValueOrDefault_Should_Return_The_Item_If_It_Exists_Without_Setting_The_Value()
    {
        var dictionary = new Dictionary<int, TestClass>
        {
            {1, new TestClass { Id = "1" }}
        };

        Assert.Same(dictionary[1], dictionary.GetValueOrDefault(1, _ => null, false));
    }

    [Fact]
    public void TryGetValueOrDefault_Should_Return_Default_If_Missing_With_Setting_The_Value()
    {
        var dictionary = new Dictionary<int, TestClass>();
        var defaultValue = new TestClass { Id = "1" };
        Assert.False(dictionary.TryGetValueOrDefault(1, _ => defaultValue, true, out var v));
        Assert.Same(defaultValue, v);
        Assert.Same(v, dictionary[1]);
    }

    [Fact]
    public void GetValueOrDefault_Should_Return_Default_If_Missing_Without_Setting_The_Value()
    {
        var dictionary = new Dictionary<int, TestClass>();
        var defaultValue = new TestClass { Id = "1" };
        Assert.Same(defaultValue, dictionary.GetValueOrDefault(1, _ => defaultValue, false));
    }

    [Fact]
    public void GetValueOrDefault_Should_Return_Default_If_Null_Without_Setting_The_Value()
    {
        var dictionary = new Dictionary<int, TestClass> { { 1, null } };
        var defaultValue = new TestClass { Id = "1" };
        Assert.Same(defaultValue, dictionary.GetValueOrDefault(1, _ => defaultValue, false));
    }
}

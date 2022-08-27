using System.Reflection;
using System.Text.Json;
using AdaptArch.Common.Utilities.Configuration.Contracts;
using AdaptArch.Common.Utilities.Configuration.Implementation;

namespace AdaptArch.Common.Utilities.UnitTests.Configuration;

public class JsonConfigurationParserSpecs
{
    private readonly IConfigurationParser _parser = new JsonConfigurationParser(":");
    [Fact]
    public void Should_Throw_If_Delimiter_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(() => _ = new JsonConfigurationParser(null!));
    }

    [Fact]
    public void Should_Throw_If_Top_Level_Is_Not_Object()
    {
        Assert.Throws<FormatException>(() => _ = _parser.Parse("[]") );
    }

    [Fact]
    public void Should_Throw_If_Value_Is_Undefined()
    {
        Assert.Throws<FormatException>(() =>
        {
            try
            {
                _parser.GetType()
                    !.GetMethod("VisitValue", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .Invoke(_parser, new object[] { new JsonElement() });
            }
            catch (Exception e)
            {
                throw e.InnerException!;
            }
        });
    }

    [Fact]
    public void Should_Throw_If_Duplicate_Key()
    {
        Assert.Throws<FormatException>(() => _ = _parser.Parse("{\"key\": 1, \"key\": 2}"));
    }

    [Fact]
    public void Should_Parse_The_Configuration()
    {
        var parsed = _parser.Parse(@"{
  'boolean': {
    'true': true,
    'false': false
  },
  'string': 'string',
  'null': null,
  'null_empty': {},
  'array': [1.2, null],
  'array_null': []
}".Replace('\'', '"'));

        Assert.NotNull(parsed);
        Assert.Equal(Boolean.TrueString, parsed["boolean:true"]);
        Assert.Equal(Boolean.FalseString, parsed["boolean:false"]);
        Assert.Equal("string", parsed["string"]);
        Assert.Equal(String.Empty, parsed["null"]);
        Assert.Null(parsed["null_empty"]);
        Assert.Equal("1.2", parsed["array:0"]);
        Assert.Equal(String.Empty, parsed["array:1"]);
        Assert.Null(parsed["array_null"]);
    }
}

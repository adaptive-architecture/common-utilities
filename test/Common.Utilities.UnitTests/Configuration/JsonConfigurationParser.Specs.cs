using System.Reflection;
using System.Text.Json;
using AdaptArch.Common.Utilities.Configuration.Contracts;
using AdaptArch.Common.Utilities.Configuration.Implementation;

namespace AdaptArch.Common.Utilities.UnitTests.Configuration;

public class JsonConfigurationParserSpecs
{
    private readonly IConfigurationParser _parser = new JsonConfigurationParser(":");

    private readonly string _jsonConfiguration = @"{
  'boolean': {
    'true': true,
    'false': false
  },
  'string': 'string',
  'null': null,
  'null_empty': {},
  'array': [1.2, null],
  'array_null': []
}".Replace('\'', '"');

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
        AssertParsedConfigurationIsValid(_parser.Parse(_jsonConfiguration));
    }

    [Fact]
    public void Should_Allow_Reuse_To_Parse_The_Configuration()
    {
        foreach (var _ in Enumerable.Range(0, 10))
        {
            AssertParsedConfigurationIsValid(_parser.Parse(_jsonConfiguration));
        }
    }

    private static void AssertParsedConfigurationIsValid(IReadOnlyDictionary<string, string> configurationValues)
    {
        Assert.NotNull(configurationValues);
        var count = configurationValues.Count;
        Assert.Equal(8, count);
        Assert.Equal(Boolean.TrueString, configurationValues["boolean:true"]);
        Assert.Equal(Boolean.FalseString, configurationValues["boolean:false"]);
        Assert.Equal("string", configurationValues["string"]);
        Assert.Equal(String.Empty, configurationValues["null"]);
        Assert.Null(configurationValues["null_empty"]);
        Assert.Equal("1.2", configurationValues["array:0"]);
        Assert.Equal(String.Empty, configurationValues["array:1"]);
        Assert.Null(configurationValues["array_null"]);
    }
}

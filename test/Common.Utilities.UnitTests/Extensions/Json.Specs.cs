using AdaptArch.Common.Utilities.Extensions;

namespace AdaptArch.Common.Utilities.UnitTests.Extensions;

public class JsonSpecs
{
    private static readonly Dictionary<string, bool> s_jsonNumbers = new()
    {
        {"1", true},
        {"1.0", true},
        {"1.1", true},
        {"1.1e-1", true},
        {"1.1e+1", true},
        {"1.1e1", true},
        {"1.1E+1", true},
        {"1.1E1", true},
        {"1.1e1+3", false},
        {"1.1e", false},
        {"1.1ea", false},
        {"127.0.0.1", false},
        {"-1.1e1", true}
    };

    private static readonly Dictionary<string, bool> s_jsonSamples = new(s_jsonNumbers)
    {
        {"", false},
        {"true", true},
        {"false", true},
        {"null", true},
        {"\"green\"", true},
        {"green", false},
        {"[1,2,3]", true},
        {"{\"key\":\"value\"}", true},
        {"[1,2,3,{\"key\":\"value\", \"prop\": {\"foo\":\"bar\"}}]", true},
        {"[1,2,3,{\"key\":\"value\", \"prop\": {foo:\"bar\"}}]", false}
    };

    [Fact]
    public void Is_Json()
    {
        foreach (var sample in s_jsonSamples)
        {
            if (sample.Value)
            {
                Assert.True(sample.Key.IsJson(), $"Expected '{sample.Key}' to be valid JSON.");
            }
            else
            {
                Assert.False(sample.Key.IsJson(), $"Expected '{sample.Key}' to be invalid JSON.");
            }
        }
    }
}

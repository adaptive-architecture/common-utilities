using AdaptArch.Common.Utilities.AspNetCore.UnitTests.Fixtures;
using Xunit;

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.ResponseTransform;

[Collection(TestServerCollection.CollectionName)]
public class ResponseTransformMiddlewareSpecs
{
    private readonly TestServerFixture _fixture;

    public ResponseTransformMiddlewareSpecs(TestServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task It_Should_Not_Transform_The_Ping_Route()
    {
        var client = _fixture.GetClient();
        var response = await client.GetAsync("/ping");

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Equal("pong", responseContent);
    }

    [Fact]
    public async Task It_Should_Transform_The_Transformed_Route()
    {
        var client = _fixture.GetClient();
        var response = await client.GetAsync("/transformed");

        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Equal("transformed", responseContent.Trim());
    }
}

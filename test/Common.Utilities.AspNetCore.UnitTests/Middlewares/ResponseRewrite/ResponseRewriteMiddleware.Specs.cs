using AdaptArch.Common.Utilities.AspNetCore.UnitTests.Fixtures;
using Xunit;

namespace AdaptArch.Common.Utilities.AspNetCore.UnitTests.Middlewares.ResponseRewrite;

[Collection(TestServerCollection.CollectionName)]
public class ResponseRewriteMiddlewareSpecs
{
    private readonly TestServerFixture _fixture;

    public ResponseRewriteMiddlewareSpecs(TestServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task It_Should_Not_Transform_The_Ping_Route()
    {
        var client = _fixture.GetClient();
        var response = await client.GetAsync("/ping", TestContext.Current.CancellationToken);

        _ = response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal("pong", responseContent);
    }

    [Fact]
    public async Task It_Should_Transform_The_Transformed_Route()
    {
        var client = _fixture.GetClient();
        var response = await client.GetAsync("/transformed", TestContext.Current.CancellationToken);

        _ = response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal("transformed", responseContent);
    }

    [Fact]
    public async Task It_Should_Not_Transform_The_Transformed_Route_If_Skipped()
    {
        var client = _fixture.GetClient();
        var response = await client.GetAsync("/transformed?skip-rewrite", TestContext.Current.CancellationToken);

        _ = response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Equal("NOT transformed", responseContent.Trim());
    }

    [Fact]
    public async Task It_Should_Not_Fail_When_No_Content_Is_Present()
    {
        var client = _fixture.GetClient();
        var response = await client.GetAsync("/no-content", TestContext.Current.CancellationToken);

        _ = response.EnsureSuccessStatusCode();
        Assert.Equal(0, response.Content.Headers.ContentLength);
        Assert.Equal(204, (int)response.StatusCode);
    }

    [Fact]
    public async Task It_Should_Not_Fail_When_Testing_Body_Methods()
    {
        var client = _fixture.GetClient();
        var response = await client.GetAsync("/methods-tests", TestContext.Current.CancellationToken);

        _ = response.EnsureSuccessStatusCode();
        Assert.Equal(0, response.Content.Headers.ContentLength);
        Assert.Equal(200, (int)response.StatusCode);
    }

    [Fact]
    public async Task It_Should_Not_Hide_Failures()
    {
        var client = _fixture.GetClient();
        var response = await client.GetAsync("/failure", TestContext.Current.CancellationToken);

        Assert.Equal(500, (int)response.StatusCode);
    }
}

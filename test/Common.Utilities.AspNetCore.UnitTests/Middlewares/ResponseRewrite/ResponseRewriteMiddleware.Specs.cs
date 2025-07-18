﻿using AdaptArch.Common.Utilities.AspNetCore.UnitTests.Fixtures;
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
        var response = await client.GetAsync("/ping");

        _ = response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Equal("pong", responseContent);
    }

    [Fact]
    public async Task It_Should_Transform_The_Transformed_Route()
    {
        var client = _fixture.GetClient();
        var response = await client.GetAsync("/transformed");

        _ = response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Equal("transformed", responseContent);
    }

    [Fact]
    public async Task It_Should_Not_Transform_The_Transformed_Route_If_Skipped()
    {
        var client = _fixture.GetClient();
        var response = await client.GetAsync("/transformed?skip-rewrite");

        _ = response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Equal("NOT transformed", responseContent.Trim());
    }

    [Fact]
    public async Task It_Should_Not_Fail_When_No_Content_Is_Present()
    {
        var client = _fixture.GetClient();
        var response = await client.GetAsync("/no-content");

        _ = response.EnsureSuccessStatusCode();
        Assert.Equal(0, response.Content.Headers.ContentLength);
        Assert.Equal(204, (int)response.StatusCode);
    }

    [Fact]
    public async Task It_Should_Not_Fail_When_Testing_Body_Methods()
    {
        var client = _fixture.GetClient();
        var response = await client.GetAsync("/methods-tests");

        _ = response.EnsureSuccessStatusCode();
        Assert.Equal(0, response.Content.Headers.ContentLength);
        Assert.Equal(200, (int)response.StatusCode);
    }

    [Fact]
    public async Task It_Should_Not_Hide_Failures()
    {
        var client = _fixture.GetClient();
        var response = await client.GetAsync("/failure");

        Assert.Equal(500, (int)response.StatusCode);
    }
}

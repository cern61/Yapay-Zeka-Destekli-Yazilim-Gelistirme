using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Backend.Tests.Infrastructure;

namespace Backend.Tests.Endpoints;

// Akış uç noktaları (/api/feed): yetki kontrolü ve bozuk cursor senaryoları.
public class FeedEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public FeedEndpointsTests(ApiTestFactory factory) => _factory = factory;

    [Fact]
    public async Task HomeFeed_WithoutToken_ReturnsUnauthorized()
    {
        // Kişisel akış (takip edilenlerin tweetleri) giriş yapmadan görülememeli.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/feed/home");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("bozuk-cursor!!")]
    [InlineData("YWJj")]
    public async Task PublicFeed_InvalidCursor_ReturnsValidationError(string cursor)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/api/feed/public?cursor={Uri.EscapeDataString(cursor)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("cursor", out _));
    }

    [Fact]
    public async Task HomeFeed_InvalidCursor_ReturnsValidationError()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiTestFactory.CreateValidToken());

        var response = await client.GetAsync("/api/feed/home?cursor=bozuk-cursor!!");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

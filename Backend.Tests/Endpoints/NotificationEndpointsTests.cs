using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Backend.Tests.Infrastructure;

namespace Backend.Tests.Endpoints;

// Bildirim uç noktaları (/api/notifications): yetki kontrolü, bozuk cursor ve rota kontrolü.
// Buradaki tüm senaryolar veritabanına gitmeden cevaplanır.
public class NotificationEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public NotificationEndpointsTests(ApiTestFactory factory) => _factory = factory;

    private HttpClient CreateAuthorizedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiTestFactory.CreateValidToken());
        return client;
    }

    // ---- Yetki kontrolü: bildirimler kişiye özeldir ----

    [Theory]
    [InlineData("/api/notifications")]
    [InlineData("/api/notifications/unread-count")]
    public async Task Get_WithoutToken_ReturnsUnauthorized(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MarkAllRead_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsync("/api/notifications/read", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MarkRead_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsync($"/api/notifications/{Guid.NewGuid()}/read", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- Sayfalama: bozuk cursor ----

    [Theory]
    [InlineData("bozuk-cursor!!")]
    [InlineData("YWJj")]
    public async Task Get_InvalidCursor_ReturnsValidationError(string cursor)
    {
        var client = CreateAuthorizedClient();

        var response = await client.GetAsync($"/api/notifications?cursor={Uri.EscapeDataString(cursor)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("cursor", out _));
    }

    // ---- Rota kontrolü ----

    [Fact]
    public async Task MarkRead_InvalidIdFormat_ReturnsNotFound()
    {
        // Rota yalnızca Guid formatındaki bildirim id'lerini kabul ediyor.
        var client = CreateAuthorizedClient();

        var response = await client.PutAsync("/api/notifications/bu-bir-guid-degil/read", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

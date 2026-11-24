using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Contracts;
using Backend.Tests.Infrastructure;

namespace Backend.Tests.Endpoints;

// Mesajlaşma uç noktaları (/api/messages): yetki kontrolü, içerik doğrulaması ve bozuk cursor.
// Buradaki tüm senaryolar veritabanına gitmeden cevaplanır.
public class MessageEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public MessageEndpointsTests(ApiTestFactory factory) => _factory = factory;

    private HttpClient CreateAuthorizedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiTestFactory.CreateValidToken());
        return client;
    }

    private static async Task AssertValidationErrorFor(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty(field, out _),
            $"Yanıtta '{field}' alanı için doğrulama hatası bekleniyordu.");
    }

    // ---- Yetki kontrolü: mesajlar özeldir, giriş yapmadan hiçbir işlem yapılamamalı ----

    [Theory]
    [InlineData("/api/messages")]
    [InlineData("/api/messages/unread-count")]
    [InlineData("/api/messages/baska_kullanici")]
    public async Task Get_WithoutToken_ReturnsUnauthorized(string url)
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Send_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/messages/baska_kullanici", new SendMessageRequest("Merhaba"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- İçerik doğrulaması: 1-1000 karakter kuralı ----

    [Theory]
    [InlineData("")]
    [InlineData("     ")]
    [InlineData("\n\t ")]
    public async Task Send_EmptyOrWhitespaceContent_ReturnsValidationError(string content)
    {
        var client = CreateAuthorizedClient();

        var response = await client.PostAsJsonAsync(
            "/api/messages/baska_kullanici", new SendMessageRequest(content));

        await AssertValidationErrorFor(response, "content");
    }

    [Fact]
    public async Task Send_NullContent_ReturnsValidationError()
    {
        var client = CreateAuthorizedClient();

        var response = await client.PostAsJsonAsync(
            "/api/messages/baska_kullanici", new { content = (string?)null });

        await AssertValidationErrorFor(response, "content");
    }

    [Fact]
    public async Task Send_1001Characters_ReturnsValidationError()
    {
        var client = CreateAuthorizedClient();

        var response = await client.PostAsJsonAsync(
            "/api/messages/baska_kullanici", new SendMessageRequest(new string('a', 1001)));

        await AssertValidationErrorFor(response, "content");
    }

    [Fact]
    public async Task Send_1001Emojis_ReturnsValidationError()
    {
        // Sunucu karakterleri "rune" olarak sayar; 1001 emoji 1001 karakterdir ve reddedilmelidir.
        var client = CreateAuthorizedClient();
        var content = string.Concat(Enumerable.Repeat("😀", 1001));

        var response = await client.PostAsJsonAsync(
            "/api/messages/baska_kullanici", new SendMessageRequest(content));

        await AssertValidationErrorFor(response, "content");
    }

    // ---- Sayfalama: bozuk cursor ----

    [Theory]
    [InlineData("bozuk-cursor!!")]
    [InlineData("YWJj")]
    public async Task GetConversation_InvalidCursor_ReturnsValidationError(string cursor)
    {
        var client = CreateAuthorizedClient();

        var response = await client.GetAsync(
            $"/api/messages/baska_kullanici?cursor={Uri.EscapeDataString(cursor)}");

        await AssertValidationErrorFor(response, "cursor");
    }
}

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Contracts;
using Backend.Tests.Infrastructure;

namespace Backend.Tests.Endpoints;

// Tweet uç noktaları (/api/tweets): yetki kontrolü ve içerik doğrulaması.
// Buradaki tüm senaryolar veritabanına gitmeden cevaplanır.
public class TweetEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public TweetEndpointsTests(ApiTestFactory factory) => _factory = factory;

    private HttpClient CreateAuthorizedClient(string? token = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token ?? ApiTestFactory.CreateValidToken());
        return client;
    }

    private static async Task AssertValidationErrorFor(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty(field, out _),
            $"Yanıtta '{field}' alanı için doğrulama hatası bekleniyordu.");
    }

    // ---- Yetki kontrolü: token olmadan korumalı işlemler yapılamamalı ----

    [Fact]
    public async Task CreateTweet_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tweets", new CreateTweetRequest("Merhaba", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTweet_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/api/tweets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LikeTweet_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PutAsync($"/api/tweets/{Guid.NewGuid()}/like", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnlikeTweet_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync($"/api/tweets/{Guid.NewGuid()}/like");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTweet_TokenSignedWithWrongSecret_ReturnsUnauthorized()
    {
        // Başka bir anahtarla imzalanmış (sahte) token kabul edilmemeli.
        var fakeToken = ApiTestFactory.CreateCustomToken(
            "tamamen-farkli-bir-secret-bu-da-32-byte-uzun",
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(30));
        var client = CreateAuthorizedClient(fakeToken);

        var response = await client.PostAsJsonAsync("/api/tweets", new CreateTweetRequest("Merhaba", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTweet_ExpiredToken_ReturnsUnauthorized()
    {
        // Süresi 5 dakika önce dolmuş token (sunucu 30 sn tolerans tanıyor).
        var expiredToken = ApiTestFactory.CreateCustomToken(
            ApiTestFactory.Secret,
            DateTime.UtcNow.AddMinutes(-40),
            DateTime.UtcNow.AddMinutes(-5));
        var client = CreateAuthorizedClient(expiredToken);

        var response = await client.PostAsJsonAsync("/api/tweets", new CreateTweetRequest("Merhaba", null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- İçerik doğrulaması: 1-280 karakter kuralı ----

    [Theory]
    [InlineData("")]
    [InlineData("     ")]
    [InlineData("\n\t ")]
    public async Task CreateTweet_EmptyOrWhitespaceContent_ReturnsValidationError(string content)
    {
        var client = CreateAuthorizedClient();

        var response = await client.PostAsJsonAsync("/api/tweets", new CreateTweetRequest(content, null));

        await AssertValidationErrorFor(response, "content");
    }

    [Fact]
    public async Task CreateTweet_NullContent_ReturnsValidationError()
    {
        var client = CreateAuthorizedClient();

        var response = await client.PostAsJsonAsync("/api/tweets", new { content = (string?)null });

        await AssertValidationErrorFor(response, "content");
    }

    [Fact]
    public async Task CreateTweet_281Characters_ReturnsValidationError()
    {
        var client = CreateAuthorizedClient();

        var response = await client.PostAsJsonAsync(
            "/api/tweets", new CreateTweetRequest(new string('a', 281), null));

        await AssertValidationErrorFor(response, "content");
    }

    [Fact]
    public async Task CreateTweet_281Emojis_ReturnsValidationError()
    {
        // Emojiler C#'ta 2 char yer kaplar; sunucu karakterleri "rune" olarak saydığı için
        // 281 emoji yine 281 karakter sayılmalı ve reddedilmeli.
        var client = CreateAuthorizedClient();
        var content = string.Concat(Enumerable.Repeat("😀", 281));

        var response = await client.PostAsJsonAsync("/api/tweets", new CreateTweetRequest(content, null));

        await AssertValidationErrorFor(response, "content");
    }

    // ---- Rota kontrolü ----

    [Fact]
    public async Task GetTweet_InvalidIdFormat_ReturnsNotFound()
    {
        // Rota yalnızca Guid formatındaki id'leri kabul ediyor.
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/tweets/bu-bir-guid-degil");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

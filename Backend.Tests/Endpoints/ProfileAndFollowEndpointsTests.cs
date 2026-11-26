using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Tests.Infrastructure;

namespace Backend.Tests.Endpoints;

// Profil güncelleme (/api/users/me) ve takip (/api/users/{username}/follow) uç noktaları:
// yetki kontrolü ve profil alanlarının doğrulaması.
// Buradaki tüm senaryolar veritabanına gitmeden cevaplanır.
public class ProfileAndFollowEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public ProfileAndFollowEndpointsTests(ApiTestFactory factory) => _factory = factory;

    private HttpClient CreateAuthorizedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", ApiTestFactory.CreateValidToken());
        return client;
    }

    // Geçerli bir profil isteği; her test yalnızca tek bir alanı bozar.
    private static Dictionary<string, string?> ValidProfile() => new()
    {
        ["displayName"] = "Test Kullanıcı",
        ["bio"] = "Merhaba",
        ["location"] = "İstanbul",
        ["website"] = "https://example.com",
        ["avatarUrl"] = "https://example.com/avatar.png"
    };

    private async Task<HttpResponseMessage> UpdateProfileWith(string field, string? value)
    {
        var profile = ValidProfile();
        profile[field] = value;
        return await CreateAuthorizedClient().PutAsJsonAsync("/api/users/me", profile);
    }

    private static async Task<JsonElement> ReadErrors(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("errors").Clone();
    }

    private static async Task AssertValidationErrorFor(HttpResponseMessage response, string field)
    {
        var errors = await ReadErrors(response);
        Assert.True(errors.TryGetProperty(field, out _),
            $"Yanıtta '{field}' alanı için doğrulama hatası bekleniyordu.");
    }

    // ---- Yetki kontrolü: takip işlemleri giriş gerektirir ----

    [Fact]
    public async Task Follow_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsync("/api/users/baska_kullanici/follow", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Unfollow_WithoutToken_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync("/api/users/baska_kullanici/follow");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ---- Görünen ad: 1-50 karakter, zorunlu ----

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("    ")]
    public async Task UpdateProfile_MissingDisplayName_ReturnsValidationError(string? displayName)
    {
        var response = await UpdateProfileWith("displayName", displayName);

        await AssertValidationErrorFor(response, "displayName");
    }

    [Fact]
    public async Task UpdateProfile_DisplayName51Characters_ReturnsValidationError()
    {
        var response = await UpdateProfileWith("displayName", new string('a', 51));

        await AssertValidationErrorFor(response, "displayName");
    }

    // ---- Biyografi ve konum: uzunluk sınırları ----

    [Fact]
    public async Task UpdateProfile_Bio161Characters_ReturnsValidationError()
    {
        var response = await UpdateProfileWith("bio", new string('a', 161));

        await AssertValidationErrorFor(response, "bio");
    }

    [Fact]
    public async Task UpdateProfile_Location31Characters_ReturnsValidationError()
    {
        var response = await UpdateProfileWith("location", new string('a', 31));

        await AssertValidationErrorFor(response, "location");
    }

    // ---- Web sitesi ve avatar: yalnızca http/https adresleri ----

    [Theory]
    [InlineData("bu bir adres degil")]
    [InlineData("example.com")]               // şema (https://) yok
    [InlineData("ftp://example.com")]         // http/https değil
    [InlineData("javascript:alert(1)")]       // güvenlik: tıklanınca kod çalıştıran bağlantı
    public async Task UpdateProfile_InvalidWebsite_ReturnsValidationError(string website)
    {
        var response = await UpdateProfileWith("website", website);

        await AssertValidationErrorFor(response, "website");
    }

    [Fact]
    public async Task UpdateProfile_Website101Characters_ReturnsValidationError()
    {
        var longUrl = "https://example.com/" + new string('a', 81); // toplam 101 karakter

        var response = await UpdateProfileWith("website", longUrl);

        await AssertValidationErrorFor(response, "website");
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html,<script>alert(1)</script>")]
    [InlineData("resim.png")]
    public async Task UpdateProfile_InvalidAvatarUrl_ReturnsValidationError(string avatarUrl)
    {
        var response = await UpdateProfileWith("avatarUrl", avatarUrl);

        await AssertValidationErrorFor(response, "avatarUrl");
    }

    // ---- Birden fazla hata aynı anda bildirilmeli ----

    [Fact]
    public async Task UpdateProfile_SeveralInvalidFields_ReportsAllErrors()
    {
        var client = CreateAuthorizedClient();
        var request = new Dictionary<string, string?>
        {
            ["displayName"] = "",
            ["bio"] = new string('a', 161),
            ["location"] = new string('a', 31),
            ["website"] = "ftp://example.com",
            ["avatarUrl"] = "javascript:alert(1)"
        };

        var response = await client.PutAsJsonAsync("/api/users/me", request);

        var errors = await ReadErrors(response);
        foreach (var field in new[] { "displayName", "bio", "location", "website", "avatarUrl" })
        {
            Assert.True(errors.TryGetProperty(field, out _), $"'{field}' hatası eksik.");
        }
    }
}

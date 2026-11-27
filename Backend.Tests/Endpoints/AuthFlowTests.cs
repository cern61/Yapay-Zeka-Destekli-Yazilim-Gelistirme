using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Contracts;
using Backend.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.Endpoints;

// Kayıt ve giriş akışının BAŞARILI senaryoları.
// İpek'in AuthTestFactory'si kullanılır: her test bellekte yeni ve boş bir SQLite veritabanı açar,
// test bitince veritabanı silinir. Gerçek (ortak) veritabanına hiç dokunulmaz.
public class AuthFlowTests
{
    private const string Password = "GucluSifre123";

    private static RegisterRequest NewUser(string username = "ali_test", string email = "ali@example.com") =>
        new(username, "Ali Test", email, Password);

    private static async Task<AuthResponse> RegisterAsync(HttpClient client, RegisterRequest request)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
    }

    private static async Task AssertValidationErrorFor(HttpResponseMessage response, string field)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty(field, out _),
            $"Yanıtta '{field}' alanı için doğrulama hatası bekleniyordu.");
    }

    // ---- Kayıt ----

    [Fact]
    public async Task Register_ValidData_ReturnsCreatedWithTokenAndProfile()
    {
        using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal("/api/users/ali_test", response.Headers.Location?.OriginalString);
        var body = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.True(body.ExpiresAt > DateTime.UtcNow);
        Assert.Equal("ali_test", body.User.Username);
        Assert.Equal("Ali Test", body.User.DisplayName);
        Assert.Equal(0, body.User.TweetCount);
        Assert.Equal(0, body.User.FollowerCount);
        Assert.Equal(0, body.User.FollowingCount);
    }

    [Fact]
    public async Task Register_ResponseDoesNotLeakPassword()
    {
        using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", NewUser());
        var json = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain(Password, json);
        Assert.DoesNotContain("passwordHash", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_PasswordIsStoredAsBcryptHash()
    {
        // Güvenlik: şifre veritabanına düz metin olarak değil, BCrypt hash olarak yazılmalı.
        using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();
        await RegisterAsync(client, NewUser());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.SingleAsync(candidate => candidate.Username == "ali_test");

        Assert.NotEqual(Password, user.PasswordHash);
        Assert.StartsWith("$2", user.PasswordHash);
        Assert.True(BCrypt.Net.BCrypt.Verify(Password, user.PasswordHash));
    }

    [Fact]
    public async Task Register_TokenCanBeUsedOnProtectedEndpoint()
    {
        // Kayıtta verilen token gerçekten çalışmalı: korumalı bir uç noktada (profil güncelleme) kullanılır.
        using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();
        var auth = await RegisterAsync(client, NewUser());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await client.PutAsJsonAsync("/api/users/me",
            new UpdateProfileRequest("Yeni Ad", "Test bio", "İstanbul", null, null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var profile = (await response.Content.ReadFromJsonAsync<UserProfile>())!;
        Assert.Equal("Yeni Ad", profile.DisplayName);
        Assert.Equal("Test bio", profile.Bio);
    }

    [Theory]
    [InlineData("ali_test")]
    [InlineData("ALI_TEST")]   // büyük/küçük harf farkı aynı kullanıcı adı sayılmalı
    public async Task Register_DuplicateUsername_ReturnsValidationError(string secondUsername)
    {
        using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();
        await RegisterAsync(client, NewUser());

        var response = await client.PostAsJsonAsync("/api/auth/register",
            NewUser(username: secondUsername, email: "baska@example.com"));

        await AssertValidationErrorFor(response, "username");
    }

    [Theory]
    [InlineData("ali@example.com")]
    [InlineData("ALI@Example.com")]   // e-posta da büyük/küçük harf duyarsız olmalı
    public async Task Register_DuplicateEmail_ReturnsValidationError(string secondEmail)
    {
        using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();
        await RegisterAsync(client, NewUser());

        var response = await client.PostAsJsonAsync("/api/auth/register",
            NewUser(username: "baska_kullanici", email: secondEmail));

        await AssertValidationErrorFor(response, "email");
    }

    // ---- Giriş ----

    [Theory]
    [InlineData("ali_test")]          // kullanıcı adıyla
    [InlineData("ali@example.com")]   // e-postayla
    [InlineData("  ali_test  ")]      // baştaki/sondaki boşluklar kırpılmalı
    public async Task Login_ValidCredentials_ReturnsToken(string login)
    {
        using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();
        await RegisterAsync(client, NewUser());

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(login, Password));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = (await response.Content.ReadFromJsonAsync<AuthResponse>())!;
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.Equal("ali_test", body.User.Username);
    }

    [Fact]
    public async Task Login_WrongPassword_ReturnsUnauthorized()
    {
        using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();
        await RegisterAsync(client, NewUser());

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("ali_test", "YanlisSifre99"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_PasswordIsCaseSensitive()
    {
        using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();
        await RegisterAsync(client, NewUser());

        var response = await client.PostAsJsonAsync("/api/auth/login",
            new LoginRequest("ali_test", Password.ToLowerInvariant()));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownUser_ReturnsUnauthorized()
    {
        // Olmayan kullanıcı da yanlış şifreyle aynı cevabı almalı;
        // böylece dışarıdan hangi kullanıcı adlarının kayıtlı olduğu anlaşılamaz.
        using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest("olmayan_kisi", Password));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Tests.Infrastructure;

// Uygulamayı test için bellekte ayağa kaldırır.
// Ayarlar UseSetting ile verilir; böylece Program.cs daha en başta Jwt:Secret'ı okurken bu değerleri görür.
// Bağlantı dizesi sahte: bu factory'yi kullanan testler veritabanına ulaşmadan sonuçlanan senaryolardır
// (yetki kontrolü, doğrulama hataları, bozuk cursor vb.).
public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    public const string Secret = "api-test-factory-secret-en-az-32-byte-uzunlugunda";
    public const string Issuer = "MaviTwitter";
    public const string Audience = "MaviTwitterClient";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("Jwt:Secret", Secret);
        builder.UseSetting("Jwt:Issuer", Issuer);
        builder.UseSetting("Jwt:Audience", Audience);
        builder.UseSetting("ConnectionStrings:Default", "Host=127.0.0.1;Port=1;Database=test;Username=test;Password=test");
    }

    // Uygulamanın kendi JwtTokenService'i ile geçerli bir token üretir.
    public static string CreateValidToken(Guid? userId = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = Secret,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience
            })
            .Build();
        var user = new User
        {
            Id = userId ?? Guid.NewGuid(),
            Username = "test_kullanici",
            DisplayName = "Test Kullanıcı",
            Email = "test@example.com",
            PasswordHash = "hash"
        };
        return new JwtTokenService(configuration).Create(user).Token;
    }

    // İstenen anahtar ve süre bilgisiyle elle token üretir (sahte veya süresi dolmuş token senaryoları için).
    public static string CreateCustomToken(string secret, DateTime notBefore, DateTime expires)
    {
        var userId = Guid.NewGuid().ToString();
        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: [new Claim(JwtRegisteredClaimNames.Sub, userId), new Claim(ClaimTypes.NameIdentifier, userId)],
            notBefore: notBefore,
            expires: expires,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)), SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

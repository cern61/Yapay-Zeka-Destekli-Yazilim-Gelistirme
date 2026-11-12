using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Backend.Models;
using Backend.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Tests.Services;

// JwtTokenService: giriş/kayıt sonrası üretilen token'ın içeriğini ve imzasını test eder.
public class JwtTokenServiceTests
{
    private const string Secret = "test-secret-en-az-32-byte-uzunlugunda-olmali!";
    private const string Issuer = "MaviTwitter";
    private const string Audience = "MaviTwitterClient";

    private static JwtTokenService CreateService(string secret = Secret)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Secret"] = secret,
                ["Jwt:Issuer"] = Issuer,
                ["Jwt:Audience"] = Audience
            })
            .Build();
        return new JwtTokenService(configuration);
    }

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        Username = "yusuf_test",
        DisplayName = "Yusuf Test",
        Email = "test@example.com",
        PasswordHash = "hash"
    };

    private static TokenValidationParameters ValidationParameters(string secret) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = Issuer,
        ValidateAudience = true,
        ValidAudience = Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
        ValidateLifetime = true
    };

    [Fact]
    public void Create_TokenContainsUserIdAndUsername()
    {
        var user = CreateUser();

        var (token, _) = CreateService().Create(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == user.Username);
    }

    [Fact]
    public void Create_TokenHasConfiguredIssuerAndAudience()
    {
        var (token, _) = CreateService().Create(CreateUser());
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(Issuer, jwt.Issuer);
        Assert.Contains(Audience, jwt.Audiences);
    }

    [Fact]
    public void Create_ExpiresInAbout30Minutes()
    {
        var before = DateTime.UtcNow;

        var (_, expiresAt) = CreateService().Create(CreateUser());

        var lifetime = expiresAt - before;
        Assert.InRange(lifetime, TimeSpan.FromMinutes(29), TimeSpan.FromMinutes(31));
    }

    [Fact]
    public void Create_TokenValidatesWithSameSecret()
    {
        var (token, _) = CreateService().Create(CreateUser());

        var exception = Record.Exception(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, ValidationParameters(Secret), out _));

        Assert.Null(exception);
    }

    [Fact]
    public void Create_TokenFailsValidationWithDifferentSecret()
    {
        var (token, _) = CreateService().Create(CreateUser());
        var wrongSecret = "baska-bir-secret-bu-da-32-byte-ten-uzun!!";

        Assert.ThrowsAny<SecurityTokenException>(() =>
            new JwtSecurityTokenHandler().ValidateToken(token, ValidationParameters(wrongSecret), out _));
    }
}

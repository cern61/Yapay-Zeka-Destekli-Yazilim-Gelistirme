using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Backend.Tests.Endpoints;

public class UserEndpointsTests
{
    [Fact]
    public async Task GetProfile_NonExistingUser_ReturnsNotFound()
    {
        using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/users/olmayan_kullanici_12345");

        Assert.Equal(
            HttpStatusCode.NotFound,
            response.StatusCode);
    }

    [Fact]
    public async Task UpdateProfile_WithoutToken_ReturnsUnauthorized()
    {
        using var factory = new AuthTestFactory();
        using var client = factory.CreateClient();

        var request = new
        {
            displayName = "Yeni İsim",
            bio = "Test biyografisi",
            location = "İstanbul"
        };

        var response = await client.PutAsJsonAsync(
            "/api/users/me", request);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }
}
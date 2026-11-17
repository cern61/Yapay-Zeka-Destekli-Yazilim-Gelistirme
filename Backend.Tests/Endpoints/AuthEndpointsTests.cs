
using System.Net;
using System.Net.Http.Json;
using Backend.Contracts;
using Backend.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace Backend.Tests.Endpoints;

public class AuthEndpointsTests
{

[Fact]
public async Task Register_EmptyUsername_ReturnsBadRequest()
{
    using var factory = new AuthTestFactory();
    using var client = factory.CreateClient();

    var request = new RegisterRequest(
        "",
        "Test Kullanıcı",
        "test@example.com",
        "Test12345!"
    );

    var response = await client.PostAsJsonAsync(
        "/api/auth/register", request
    );

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
   
[Fact]
public async Task Register_EmptyEmail_ReturnsBadRequest()
{
    using var factory = new AuthTestFactory();
    using var client = factory.CreateClient();

    var request = new RegisterRequest(
        "testuser",
        "Test Kullanıcı",
        "",
        "Test12345!"
    );

    var response = await client.PostAsJsonAsync(
        "/api/auth/register", request
    );

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}


[Fact]
public async Task Register_InvalidEmail_ReturnsBadRequest()
{
    using var factory = new AuthTestFactory();
    using var client = factory.CreateClient();

    var request = new RegisterRequest(
        "testuser",
        "Test Kullanıcı",
        "gecersizemail",
        "Test12345!"
    );

    var response = await client.PostAsJsonAsync(
        "/api/auth/register", request
    );

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
[Fact]
public async Task Register_EmptyPassword_ReturnsBadRequest()
{
    using var factory = new AuthTestFactory();
    using var client = factory.CreateClient();

    var request = new RegisterRequest(
        "testuser",
        "Test Kullanıcı",
        "test@example.com",
        ""
    );

    var response = await client.PostAsJsonAsync(
        "/api/auth/register", request
    );

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
 [Fact]
public async Task Login_EmptyLogin_ReturnsBadRequest()
{
    using var factory = new AuthTestFactory();
    using var client = factory.CreateClient();

    var request = new LoginRequest(
        "",
        "Test12345!"
    );

    var response = await client.PostAsJsonAsync(
        "/api/auth/login", request
    );

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
[Fact]
public async Task Login_EmptyPassword_ReturnsBadRequest()
{
    using var factory = new AuthTestFactory();
    using var client = factory.CreateClient();

    var request = new LoginRequest(
        "testuser",
        ""
    );

    var response = await client.PostAsJsonAsync(
        "/api/auth/login", request
    );

    Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
}
}



public class AuthTestFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["Jwt:Secret"] = "TestOnlySecretKey12345678901234567890",
                    ["ConnectionStrings:Default"] =
                        "Host=localhost;Database=test;Username=test;Password=test"
                });
        });
    }

}

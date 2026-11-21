
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
using Microsoft.Data.Sqlite;
using Microsoft.AspNetCore.TestHost;

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

        builder.UseSetting("Jwt:Secret",
            "TestOnlySecretKey12345678901234567890");

        builder.UseSetting("ConnectionStrings:Default",
            "Host=127.0.0.1;Port=1;Database=test;Username=test;Password=test");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();

            services.AddSingleton<SqliteConnection>(_ =>
            {
                var connection = new SqliteConnection("Data Source=:memory:");
                connection.Open();
                return connection;
            });

            services.AddDbContext<AppDbContext>((provider, options) =>
            {
                var connection = provider.GetRequiredService<SqliteConnection>();
                options.UseSqlite(connection);
            });
        });
    }
protected override Microsoft.Extensions.Hosting.IHost CreateHost(
    Microsoft.Extensions.Hosting.IHostBuilder builder)
{
    var host = base.CreateHost(builder);

    using var scope = host.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    if (!db.Database.IsSqlite())
        throw new InvalidOperationException("SQLite test database required.");

    db.Database.EnsureCreated();

    return host;
}}
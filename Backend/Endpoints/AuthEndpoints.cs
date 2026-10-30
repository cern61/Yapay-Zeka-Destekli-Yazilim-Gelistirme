using System.Text;
using System.Text.RegularExpressions;
using Backend.Contracts;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend.Endpoints;

public static partial class AuthEndpoints
{
    [GeneratedRegex("^[A-Za-z0-9_]{3,15}$", RegexOptions.CultureInvariant)]
    private static partial Regex UsernamePattern();

    [GeneratedRegex("^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$", RegexOptions.CultureInvariant)]
    private static partial Regex EmailPattern();

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth").WithTags("Authentication");
        group.MapPost("/register", RegisterAsync);
        group.MapPost("/login", LoginAsync);
        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request, AppDbContext db, JwtTokenService tokens, CancellationToken cancellationToken)
    {
        var username = request.Username?.Trim();
        var displayName = request.DisplayName?.Trim();
        var email = request.Email?.Trim();
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrEmpty(username) || !UsernamePattern().IsMatch(username))
        {
            errors["username"] = ["Kullanıcı adı 3-15 karakter olmalı; yalnızca harf, rakam ve _ içerebilir."];
        }
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 50)
        {
            errors["displayName"] = ["Görünen ad 1-50 karakter olmalıdır."];
        }
        if (string.IsNullOrEmpty(email) || email.Length > 254 || !EmailPattern().IsMatch(email))
        {
            errors["email"] = ["Geçerli bir e-posta adresi girin (en fazla 254 karakter)."];
        }
        if (string.IsNullOrWhiteSpace(request.Password)
            || request.Password.Length < 8
            || Encoding.UTF8.GetByteCount(request.Password) > 72)
        {
            errors["password"] = ["Şifre en az 8 karakter ve BCrypt için en fazla 72 UTF-8 bayt olmalıdır."];
        }
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }
var usernameExists = await db.Users.AnyAsync(
    u => u.Username == username,
    cancellationToken);

if (usernameExists)
{
    return Results.ValidationProblem(
        new Dictionary<string, string[]>
        {
            ["username"] = ["Bu kullanıcı adı zaten kullanılıyor."]
        });
}
var emailExists = await db.Users.AnyAsync(
    u => u.Email == email,
    cancellationToken);

if (emailExists)
{
    return Results.ValidationProblem(
        new Dictionary<string, string[]>
        {
            ["email"] = ["Bu e-posta adresi zaten kullanılıyor."]
        });
}
        var user = new User
        {
            Username = username!,
            DisplayName = displayName!,
            Email = email!,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12)
        };
        db.Users.Add(user);
        await db.SaveChangesAsync(cancellationToken);

        var (token, expiresAt) = tokens.Create(user);
        var profile = await ApiHelpers.ToProfileAsync(db, user, user.Id, cancellationToken);
        return Results.Created($"/api/users/{user.Username}", new AuthResponse(token, expiresAt, profile));
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request, AppDbContext db, JwtTokenService tokens, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Login) || string.IsNullOrEmpty(request.Password))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["login"] = ["Kullanıcı adı/e-posta ve şifre zorunludur."]
            });
        }

        var login = request.Login.Trim();
        var user = await db.Users.SingleOrDefaultAsync(
            candidate => candidate.Username == login || candidate.Email == login, cancellationToken);
        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Results.Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Giriş bilgileri hatalı.");
        }

        var (token, expiresAt) = tokens.Create(user);
        var profile = await ApiHelpers.ToProfileAsync(db, user, user.Id, cancellationToken);
        return Results.Ok(new AuthResponse(token, expiresAt, profile));
    }
}

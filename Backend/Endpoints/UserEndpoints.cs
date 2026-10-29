using Backend.Contracts;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/users").WithTags("Users");
        group.MapGet("/{username}", GetProfileAsync);
        group.MapPut("/me", UpdateProfileAsync).RequireAuthorization();
        group.MapGet("/{username}/tweets", GetTweetsAsync);
        group.MapGet("/{username}/followers", GetFollowersAsync);
        group.MapGet("/{username}/following", GetFollowingAsync);
        group.MapPost("/{username}/follow", FollowAsync).RequireAuthorization();
        group.MapDelete("/{username}/follow", UnfollowAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> GetProfileAsync(
        string username, HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Username == username, cancellationToken);
        if (user is null)
        {
            return Results.NotFound();
        }

        Guid? viewerId = context.User.Identity?.IsAuthenticated == true
            ? ApiHelpers.CurrentUserId(context)
            : null;
        return Results.Ok(await ApiHelpers.ToProfileAsync(db, user, viewerId, cancellationToken));
    }

    private static async Task<IResult> UpdateProfileAsync(
        UpdateProfileRequest request, HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        var displayName = request.DisplayName?.Trim();
        if (string.IsNullOrWhiteSpace(displayName) || displayName.Length > 50)
        {
            errors["displayName"] = ["Görünen ad 1-50 karakter olmalıdır."];
        }
        ValidateOptionalUrl(request.Website, "website", 100, errors);
        ValidateOptionalUrl(request.AvatarUrl, "avatarUrl", 500, errors);
        if (request.Bio?.Length > 160)
        {
            errors["bio"] = ["Biyografi en fazla 160 karakter olabilir."];
        }
        if (request.Location?.Length > 30)
        {
            errors["location"] = ["Konum en fazla 30 karakter olabilir."];
        }
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var userId = ApiHelpers.CurrentUserId(context);
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        user.DisplayName = displayName!;
        user.Bio = NormalizeOptional(request.Bio);
        user.Location = NormalizeOptional(request.Location);
        user.Website = NormalizeOptional(request.Website);
        user.AvatarUrl = NormalizeOptional(request.AvatarUrl);
        await db.SaveChangesAsync(cancellationToken);
        return Results.Ok(await ApiHelpers.ToProfileAsync(db, user, user.Id, cancellationToken));
    }

    private static async Task<IResult> GetTweetsAsync(
        string username, HttpContext context, AppDbContext db, string? cursor, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Username == username, cancellationToken);
        if (user is null)
        {
            return Results.NotFound();
        }

        var parsedCursor = cursor is null ? null : ApiHelpers.DecodeCursor(cursor);
        if (cursor is not null && parsedCursor is null)
        {
            return InvalidCursor();
        }

        var query = TweetPaging.BeforeCursor(db.Tweets.Where(tweet => tweet.UserId == user.Id), parsedCursor);
        Guid? viewerId = context.User.Identity?.IsAuthenticated == true
            ? ApiHelpers.CurrentUserId(context)
            : null;
        return Results.Ok(await TweetPaging.ReadAsync(query, db, viewerId, cancellationToken));
    }

    private static async Task<IResult> GetFollowersAsync(
        string username, AppDbContext db, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Username == username, cancellationToken);
        if (user is null)
        {
            return Results.NotFound();
        }

        var followers = await db.Follows
            .Where(follow => follow.FollowingId == user.Id)
            .OrderBy(follow => follow.CreatedAt)
            .Join(db.Users, follow => follow.FollowerId, follower => follower.Id,
                (_, follower) => new UserSummary(follower.Id, follower.Username, follower.DisplayName, follower.AvatarUrl))
            .Take(100)
            .ToListAsync(cancellationToken);
        return Results.Ok(followers);
    }

    private static async Task<IResult> GetFollowingAsync(
        string username, AppDbContext db, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(candidate => candidate.Username == username, cancellationToken);
        if (user is null)
        {
            return Results.NotFound();
        }

        var following = await db.Follows
            .Where(follow => follow.FollowerId == user.Id)
            .OrderBy(follow => follow.CreatedAt)
            .Join(db.Users, follow => follow.FollowingId, followed => followed.Id,
                (_, followed) => new UserSummary(followed.Id, followed.Username, followed.DisplayName, followed.AvatarUrl))
            .Take(100)
            .ToListAsync(cancellationToken);
        return Results.Ok(following);
    }

    private static async Task<IResult> FollowAsync(
        string username, HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        var target = await db.Users.SingleOrDefaultAsync(candidate => candidate.Username == username, cancellationToken);
        if (target is null)
        {
            return Results.NotFound();
        }

        var followerId = ApiHelpers.CurrentUserId(context);
        if (target.Id == followerId)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["username"] = ["Kendinizi takip edemezsiniz."]
            });
        }

        var exists = await db.Follows.AnyAsync(
            follow => follow.FollowerId == followerId && follow.FollowingId == target.Id, cancellationToken);
        if (!exists)
        {
            db.Follows.Add(new Follow { FollowerId = followerId, FollowingId = target.Id });
            await db.SaveChangesAsync(cancellationToken);
        }

        return Results.NoContent();
    }

    private static async Task<IResult> UnfollowAsync(
        string username, HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        var target = await db.Users.SingleOrDefaultAsync(candidate => candidate.Username == username, cancellationToken);
        if (target is null)
        {
            return Results.NotFound();
        }

        var followerId = ApiHelpers.CurrentUserId(context);
        var follow = await db.Follows.SingleOrDefaultAsync(
            item => item.FollowerId == followerId && item.FollowingId == target.Id, cancellationToken);
        if (follow is not null)
        {
            db.Follows.Remove(follow);
            await db.SaveChangesAsync(cancellationToken);
        }

        return Results.NoContent();
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void ValidateOptionalUrl(
        string? value, string field, int maxLength, IDictionary<string, string[]> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength
            || !Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            errors[field] = [$"{field} geçerli bir http:// veya https:// adresi olmalıdır (en fazla {maxLength} karakter)."];
        }
    }

    private static IResult InvalidCursor() =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["cursor"] = ["Cursor biçimi geçersiz."]
        });
}

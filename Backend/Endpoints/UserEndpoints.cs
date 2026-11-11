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
        group.MapGet("/suggestions", GetSuggestionsAsync).RequireAuthorization();
        group.MapGet("/{username}", GetProfileAsync);
        group.MapPut("/me", UpdateProfileAsync).RequireAuthorization();
        group.MapPost("/me/avatar", UploadAvatarAsync).RequireAuthorization();
        group.MapGet("/{username}/tweets", GetTweetsAsync);
        group.MapGet("/{username}/followers", GetFollowersAsync);
        group.MapGet("/{username}/following", GetFollowingAsync);
        group.MapPost("/{username}/follow", FollowAsync).RequireAuthorization();
        group.MapDelete("/{username}/follow", UnfollowAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> GetSuggestionsAsync(
    HttpContext context,
    AppDbContext db,
    CancellationToken cancellationToken)
{
    var userId = ApiHelpers.CurrentUserId(context);

    var followedUserIds = db.Follows
        .Where(follow => follow.FollowerId == userId)
        .Select(follow => follow.FollowingId);

    var suggestions = await db.Users
        .Where(user =>
            user.Id != userId &&
            !followedUserIds.Contains(user.Id))
        .OrderBy(user => user.Username)
        .Select(user => new UserSummary(
            user.Id,
            user.Username,
            user.DisplayName,
            user.AvatarUrl
        ))
        .Take(5)
        .ToListAsync(cancellationToken);

    return Results.Ok(suggestions);
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
        if (NormalizeOptional(request.Bio)?.Length > 160)
        {
            errors["bio"] = ["Biyografi en fazla 160 karakter olabilir."];
        }
        if (NormalizeOptional(request.Location)?.Length > 30)
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

    
private static async Task<IResult> UploadAvatarAsync(
    HttpContext context,
    AppDbContext db,
    SupabaseStorageService storage,
    CancellationToken cancellationToken)
{
    const long maxFileSize = 4 * 1024 * 1024;

    if (!context.Request.HasFormContentType)
    {
        return Results.BadRequest(new
        {
            message = "Multipart/form-data bekleniyor."
        });
    }

    if (context.Request.ContentLength > maxFileSize + 65536)
    {
        return Results.StatusCode(StatusCodes.Status413PayloadTooLarge);
    }

    var form = await context.Request.ReadFormAsync(cancellationToken);
    var file = form.Files.GetFile("file");

    if (file is null || file.Length == 0)
    {
        return Results.BadRequest(new { message = "Fotoğraf seçilmedi." });
    }

    if (file.Length > maxFileSize)
    {
        return Results.BadRequest(new { message = "Fotoğraf en fazla 4 MB olabilir." });
    }

    var contentType = file.ContentType.ToLowerInvariant();

    var extension = contentType switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => null
    };

    if (extension is null)
    {
        return Results.BadRequest(new
        {
            message = "Yalnızca JPEG, PNG ve WebP dosyaları kabul edilir."
        });
    }

    // Dosyanın gerçek formatını kontrol et.
    using var checkStream = file.OpenReadStream();
    var header = new byte[12];

    var bytesRead = await checkStream.ReadAsync(
        header.AsMemory(), cancellationToken);

    if (!HasValidImageSignature(header, bytesRead, contentType))
    {
        return Results.BadRequest(new
        {
            message = "Dosya içeriği geçerli bir görsel değil."
        });
    }

    var userId = ApiHelpers.CurrentUserId(context);

    var user = await db.Users.SingleOrDefaultAsync(
        candidate => candidate.Id == userId,
        cancellationToken);

    if (user is null)
    {
        return Results.Unauthorized();
    }

    await using var uploadStream = file.OpenReadStream();

    var avatarUrl = await storage.UploadAvatarAsync(
        userId,
        uploadStream,
        contentType,
        extension,
        cancellationToken);

    user.AvatarUrl = avatarUrl;

    await db.SaveChangesAsync(cancellationToken);

    return Results.Ok(
        await ApiHelpers.ToProfileAsync(
            db, user, user.Id, cancellationToken));
}

private static bool HasValidImageSignature(
    byte[] header,
    int length,
    string contentType)
{
    return contentType switch
    {
        "image/jpeg" =>
            length >= 3 &&
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF,

        "image/png" =>
            length >= 8 &&
            header[0] == 0x89 &&
            header[1] == 0x50 &&
            header[2] == 0x4E &&
            header[3] == 0x47 &&
            header[4] == 0x0D &&
            header[5] == 0x0A &&
            header[6] == 0x1A &&
            header[7] == 0x0A,

        "image/webp" =>
            length >= 12 &&
            header[0] == 'R' &&
            header[1] == 'I' &&
            header[2] == 'F' &&
            header[3] == 'F' &&
            header[8] == 'W' &&
            header[9] == 'E' &&
            header[10] == 'B' &&
            header[11] == 'P',

        _ => false
    };
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


using Backend.Data;
using Backend.Services;

namespace Backend.Endpoints;

public static class FeedEndpoints
{
    public static IEndpointRouteBuilder MapFeedEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/feed").WithTags("Feed");

        group.MapGet("/public", PublicAsync);
        group.MapGet("/home", HomeAsync).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> PublicAsync(
        HttpContext context,
        AppDbContext db,
        string? cursor,
        CancellationToken cancellationToken)
    {
        var parsedCursor = cursor is null
            ? null
            : ApiHelpers.DecodeCursor(cursor);

        if (cursor is not null && parsedCursor is null)
        {
            return InvalidCursor();
        }

        // Önce silinmiş gönderileri ve yanıtları filtrele.
        var filteredQuery = db.Tweets.Where(tweet =>
            tweet.DeletedAt == null &&
            tweet.ParentId == null
        );

        // Ardından cursor uygula.
        var query = TweetPaging.BeforeCursor(
            filteredQuery,
            parsedCursor
        );

        Guid? viewerId = context.User.Identity?.IsAuthenticated == true
            ? ApiHelpers.CurrentUserId(context)
            : null;

        return Results.Ok(
            await TweetPaging.ReadAsync(
                query,
                db,
                viewerId,
                cancellationToken
            )
        );
    }

    private static async Task<IResult> HomeAsync(
        HttpContext context,
        AppDbContext db,
        string? cursor,
        CancellationToken cancellationToken)
    {
        var parsedCursor = cursor is null
            ? null
            : ApiHelpers.DecodeCursor(cursor);

        if (cursor is not null && parsedCursor is null)
        {
            return InvalidCursor();
        }

        var userId = ApiHelpers.CurrentUserId(context);

        var followedUserIds = db.Follows
            .Where(follow => follow.FollowerId == userId)
            .Select(follow => follow.FollowingId);

        // Takip edilen kullanıcıların ve kendi gönderilerinin
        // yalnızca silinmemiş ana gönderilerini getir.
        var filteredQuery = db.Tweets.Where(tweet =>
            tweet.DeletedAt == null &&
            tweet.ParentId == null &&
            (
                tweet.UserId == userId ||
                followedUserIds.Contains(tweet.UserId)
            )
        );

        // Filtrelemeden sonra cursor uygula.
        var query = TweetPaging.BeforeCursor(
            filteredQuery,
            parsedCursor
        );

        return Results.Ok(
            await TweetPaging.ReadAsync(
                query,
                db,
                userId,
                cancellationToken
            )
        );
    }

    private static IResult InvalidCursor() =>
        Results.ValidationProblem(
            new Dictionary<string, string[]>
            {
                ["cursor"] = ["Cursor biçimi geçersiz."]
            }
        );
}

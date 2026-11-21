using Backend.Contracts;
using Backend.Data;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend.Endpoints;

public static class NotificationEndpoints
{
    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/notifications").WithTags("Notifications").RequireAuthorization();
        group.MapGet("/", GetAsync);
        group.MapGet("/unread-count", GetUnreadCountAsync);
        group.MapPut("/read", MarkAllReadAsync);
        group.MapPut("/{id:guid}/read", MarkReadAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        HttpContext context, AppDbContext db, string? cursor, CancellationToken cancellationToken)
    {
        var parsedCursor = cursor is null ? null : ApiHelpers.DecodeCursor(cursor);
        if (cursor is not null && parsedCursor is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["cursor"] = ["Cursor biçimi geçersiz."]
            });
        }

        var me = ApiHelpers.CurrentUserId(context);
        var query = db.Notifications.Where(notification => notification.RecipientId == me);
        if (parsedCursor is not null)
        {
            query = query.Where(notification => notification.CreatedAt < parsedCursor.CreatedAt
                || (notification.CreatedAt == parsedCursor.CreatedAt && notification.Id < parsedCursor.Id));
        }

        var items = await query
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Select(notification => new NotificationResponse(
                notification.Id,
                notification.Type,
                new UserSummary(notification.Actor.Id, notification.Actor.Username,
                    notification.Actor.DisplayName, notification.Actor.AvatarUrl),
                notification.TweetId,
                // silinmiş tweet de bulunsun diye IgnoreQueryFilters
                db.Tweets.IgnoreQueryFilters()
                    .Where(tweet => tweet.Id == notification.TweetId)
                    .Select(tweet => tweet.Content)
                    .FirstOrDefault(),
                notification.TweetId != null && db.Tweets.IgnoreQueryFilters()
                    .Any(tweet => tweet.Id == notification.TweetId && tweet.DeletedAt != null),
                notification.CreatedAt,
                notification.ReadAt))
            .Take(TweetPaging.PageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > TweetPaging.PageSize;
        if (hasMore)
        {
            items.RemoveAt(TweetPaging.PageSize);
        }
        var nextCursor = hasMore && items.Count > 0
            ? ApiHelpers.EncodeCursor(new TweetCursor(items[^1].CreatedAt, items[^1].Id))
            : null;
        return Results.Ok(new PageResponse<NotificationResponse>(items, nextCursor));
    }

    private static async Task<IResult> GetUnreadCountAsync(
        HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        var me = ApiHelpers.CurrentUserId(context);
        var count = await db.Notifications.CountAsync(
            notification => notification.RecipientId == me && notification.ReadAt == null, cancellationToken);
        return Results.Ok(new CountResponse(count));
    }

    private static async Task<IResult> MarkAllReadAsync(
        HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        var me = ApiHelpers.CurrentUserId(context);
        await db.Notifications
            .Where(notification => notification.RecipientId == me && notification.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                notification => notification.ReadAt, notification => (DateTime?)DateTime.UtcNow), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> MarkReadAsync(
        Guid id, HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        var me = ApiHelpers.CurrentUserId(context);
        if (!await db.Notifications.AnyAsync(
                notification => notification.Id == id && notification.RecipientId == me, cancellationToken))
        {
            return Results.NotFound();   // başkasının bildirimi de 404
        }

        await db.Notifications
            .Where(notification => notification.Id == id && notification.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                notification => notification.ReadAt, notification => (DateTime?)DateTime.UtcNow), cancellationToken);
        return Results.NoContent();
    }
}
using Backend.Contracts;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public static class TweetPaging
{
    public const int PageSize = 20;

    public static IQueryable<Tweet> BeforeCursor(IQueryable<Tweet> query, TweetCursor? cursor) =>
        cursor is null
            ? query
            : query.Where(tweet => tweet.CreatedAt < cursor.CreatedAt
                || (tweet.CreatedAt == cursor.CreatedAt && tweet.Id < cursor.Id));

    public static async Task<PageResponse<TweetResponse>> ReadAsync(
        IQueryable<Tweet> query, AppDbContext db, Guid? viewerId, CancellationToken cancellationToken)
    {
        var items = await query
            .OrderByDescending(tweet => tweet.CreatedAt)
            .ThenByDescending(tweet => tweet.Id)
            .ProjectTweets(db, viewerId)
            .Take(PageSize + 1)
            .ToListAsync(cancellationToken);

        var hasMore = items.Count > PageSize;
        if (hasMore)
        {
            items.RemoveAt(PageSize);
        }

        var nextCursor = hasMore && items.Count > 0
            ? ApiHelpers.EncodeCursor(new TweetCursor(items[^1].CreatedAt, items[^1].Id))
            : null;
        return new PageResponse<TweetResponse>(items, nextCursor);
    }
}

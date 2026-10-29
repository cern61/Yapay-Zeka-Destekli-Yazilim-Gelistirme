using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Backend.Contracts;
using Backend.Data;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public static class ApiHelpers
{
    public static Guid CurrentUserId(HttpContext context) =>
        Guid.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static async Task<UserProfile> ToProfileAsync(
        AppDbContext db, User user, Guid? viewerId, CancellationToken cancellationToken)
    {
        var tweetCount = await db.Tweets.CountAsync(tweet => tweet.UserId == user.Id, cancellationToken);
        var followerCount = await db.Follows.CountAsync(follow => follow.FollowingId == user.Id, cancellationToken);
        var followingCount = await db.Follows.CountAsync(follow => follow.FollowerId == user.Id, cancellationToken);
        var isFollowing = viewerId.HasValue && await db.Follows.AnyAsync(
            follow => follow.FollowerId == viewerId.Value && follow.FollowingId == user.Id,
            cancellationToken);

        return new UserProfile(user.Id, user.Username, user.DisplayName, user.Bio, user.Location,
            user.Website, user.AvatarUrl, user.CreatedAt, tweetCount, followerCount, followingCount, isFollowing);
    }

    public static IQueryable<TweetResponse> ProjectTweets(
        this IQueryable<Tweet> tweets, AppDbContext db, Guid? viewerId)
    {
        return tweets.Select(tweet => new TweetResponse(
            tweet.Id,
            tweet.Content,
            tweet.DeletedAt != null,
            tweet.ParentId,
            tweet.ParentId == null
                ? null
                : db.Tweets.IgnoreQueryFilters()
                    .Where(parent => parent.Id == tweet.ParentId)
                    .Select(parent => parent.Content)
                    .FirstOrDefault(),
            tweet.ParentId != null && db.Tweets.IgnoreQueryFilters()
                .Any(parent => parent.Id == tweet.ParentId && parent.DeletedAt != null),
            tweet.CreatedAt,
            new UserSummary(tweet.Author.Id, tweet.Author.Username, tweet.Author.DisplayName, tweet.Author.AvatarUrl),
            db.Likes.Count(like => like.TweetId == tweet.Id),
            db.Tweets.Count(reply => reply.ParentId == tweet.Id && reply.DeletedAt == null),
            viewerId.HasValue && db.Likes.Any(like => like.TweetId == tweet.Id && like.UserId == viewerId.Value)));
    }

    public static string EncodeCursor(TweetCursor cursor)
    {
        var bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(cursor));
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static TweetCursor? DecodeCursor(string? value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 512)
        {
            return null;
        }

        try
        {
            var base64 = value.Replace('-', '+').Replace('_', '/');
            base64 = base64.PadRight(base64.Length + ((4 - base64.Length % 4) % 4), '=');
            var cursor = JsonSerializer.Deserialize<TweetCursor>(Convert.FromBase64String(base64));
            return cursor is not null && cursor.Id != Guid.Empty && cursor.CreatedAt != default
                ? cursor
                : null;
        }
        catch (FormatException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}

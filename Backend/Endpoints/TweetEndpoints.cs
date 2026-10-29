using System.Text;
using Backend.Contracts;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend.Endpoints;

public static class TweetEndpoints
{
    public static IEndpointRouteBuilder MapTweetEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/tweets").WithTags("Tweets");
        group.MapGet("/{id:guid}", GetAsync);
        group.MapPost("/", CreateAsync).RequireAuthorization();
        group.MapDelete("/{id:guid}", DeleteAsync).RequireAuthorization();
        group.MapGet("/{id:guid}/replies", GetRepliesAsync);
        group.MapPut("/{id:guid}/like", LikeAsync).RequireAuthorization();
        group.MapDelete("/{id:guid}/like", UnlikeAsync).RequireAuthorization();
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        Guid id, HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        Guid? viewerId = context.User.Identity?.IsAuthenticated == true
            ? ApiHelpers.CurrentUserId(context)
            : null;
        var tweet = await db.Tweets.IgnoreQueryFilters()
            .Where(item => item.Id == id)
            .ProjectTweets(db, viewerId)
            .SingleOrDefaultAsync(cancellationToken);
        return tweet is null ? Results.NotFound() : Results.Ok(tweet);
    }

    private static async Task<IResult> CreateAsync(
        CreateTweetRequest request, HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        var content = request.Content?.Trim();
        var contentLength = content?.EnumerateRunes().Count() ?? 0;
        if (contentLength is < 1 or > 280)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["content"] = ["Tweet içeriği boş olmamalı ve 280 karakteri aşmamalıdır."]
            });
        }

        if (request.ParentId is { } parentId
            && !await db.Tweets.AnyAsync(tweet => tweet.Id == parentId, cancellationToken))
        {
            return Results.NotFound(new { message = "Yanıtlanacak tweet bulunamadı." });
        }

        var tweet = new Tweet
        {
            UserId = ApiHelpers.CurrentUserId(context),
            ParentId = request.ParentId,
            Content = content
        };
        db.Tweets.Add(tweet);
        await db.SaveChangesAsync(cancellationToken);
        var response = await db.Tweets.Where(item => item.Id == tweet.Id)
            .ProjectTweets(db, tweet.UserId).SingleAsync(cancellationToken);
        return Results.Created($"/api/tweets/{tweet.Id}", response);
    }

    private static async Task<IResult> DeleteAsync(
        Guid id, HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        var tweet = await db.Tweets.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (tweet is null)
        {
            return Results.NotFound();
        }
        if (tweet.UserId != ApiHelpers.CurrentUserId(context))
        {
            return Results.Forbid();
        }

        tweet.Content = null;
        tweet.DeletedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetRepliesAsync(
        Guid id, HttpContext context, AppDbContext db, string? cursor, CancellationToken cancellationToken)
    {
        if (!await db.Tweets.IgnoreQueryFilters().AnyAsync(tweet => tweet.Id == id, cancellationToken))
        {
            return Results.NotFound();
        }
        var parsedCursor = cursor is null ? null : ApiHelpers.DecodeCursor(cursor);
        if (cursor is not null && parsedCursor is null)
        {
            return InvalidCursor();
        }

        var query = TweetPaging.BeforeCursor(db.Tweets.Where(tweet => tweet.ParentId == id), parsedCursor);
        Guid? viewerId = context.User.Identity?.IsAuthenticated == true
            ? ApiHelpers.CurrentUserId(context)
            : null;
        return Results.Ok(await TweetPaging.ReadAsync(query, db, viewerId, cancellationToken));
    }

    private static async Task<IResult> LikeAsync(
        Guid id, HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Tweets.AnyAsync(tweet => tweet.Id == id, cancellationToken))
        {
            return Results.NotFound();
        }

        var userId = ApiHelpers.CurrentUserId(context);
        var exists = await db.Likes.AnyAsync(
            like => like.UserId == userId && like.TweetId == id, cancellationToken);
        if (!exists)
        {
            db.Likes.Add(new Like { UserId = userId, TweetId = id });
            await db.SaveChangesAsync(cancellationToken);
        }
        return Results.NoContent();
    }

    private static async Task<IResult> UnlikeAsync(
        Guid id, HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Tweets.IgnoreQueryFilters().AnyAsync(tweet => tweet.Id == id, cancellationToken))
        {
            return Results.NotFound();
        }

        var userId = ApiHelpers.CurrentUserId(context);
        var like = await db.Likes.SingleOrDefaultAsync(
            item => item.UserId == userId && item.TweetId == id, cancellationToken);
        if (like is not null)
        {
            db.Likes.Remove(like);
            await db.SaveChangesAsync(cancellationToken);
        }
        return Results.NoContent();
    }

    private static IResult InvalidCursor() =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["cursor"] = ["Cursor biçimi geçersiz."]
        });
}

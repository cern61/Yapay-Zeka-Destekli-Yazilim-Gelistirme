using Backend.Contracts;
using Backend.Data;
using Backend.Models;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend.Endpoints;

public static class MessageEndpoints
{
    private const int MaxConversations = 50;

    public static IEndpointRouteBuilder MapMessageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/messages").WithTags("Messages").RequireAuthorization();
        group.MapGet("/", GetConversationsAsync);
        group.MapGet("/unread-count", GetUnreadCountAsync);
        group.MapGet("/{username}", GetConversationAsync);
        group.MapPost("/{username}", SendAsync);
        return endpoints;
    }

    // Sohbet listesi: her kişi için son mesaj + okunmamış sayısı
    private static async Task<IResult> GetConversationsAsync(
        HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        var me = ApiHelpers.CurrentUserId(context);
        var conversations = await db.Users
            .Where(user => user.Id != me && db.Messages.Any(message =>
                (message.SenderId == me && message.RecipientId == user.Id)
                || (message.SenderId == user.Id && message.RecipientId == me)))
            .Select(user => new
            {
                User = new UserSummary(user.Id, user.Username, user.DisplayName, user.AvatarUrl),
                LastMessage = db.Messages
                    .Where(message => (message.SenderId == me && message.RecipientId == user.Id)
                        || (message.SenderId == user.Id && message.RecipientId == me))
                    .OrderByDescending(message => message.CreatedAt)
                    .ThenByDescending(message => message.Id)
                    .Select(message => new MessageResponse(message.Id, message.SenderId, message.RecipientId,
                        message.Content, message.CreatedAt, message.ReadAt))
                    .First(),
                LastAt = db.Messages
                    .Where(message => (message.SenderId == me && message.RecipientId == user.Id)
                        || (message.SenderId == user.Id && message.RecipientId == me))
                    .Max(message => message.CreatedAt),
                UnreadCount = db.Messages.Count(message =>
                    message.SenderId == user.Id && message.RecipientId == me && message.ReadAt == null)
            })
            .OrderByDescending(item => item.LastAt)
            .Take(MaxConversations)
            .ToListAsync(cancellationToken);

        return Results.Ok(conversations
            .Select(item => new ConversationResponse(item.User, item.LastMessage, item.UnreadCount))
            .ToList());
    }

    private static async Task<IResult> GetUnreadCountAsync(
        HttpContext context, AppDbContext db, CancellationToken cancellationToken)
    {
        var me = ApiHelpers.CurrentUserId(context);
        var count = await db.Messages.CountAsync(
            message => message.RecipientId == me && message.ReadAt == null, cancellationToken);
        return Results.Ok(new CountResponse(count));
    }

    // Sohbetin mesajları (yeniden eskiye, 20'şerli). Açılınca karşı tarafın mesajları okundu olur.
    private static async Task<IResult> GetConversationAsync(
        string username, HttpContext context, AppDbContext db, string? cursor,
        CancellationToken cancellationToken)
    {
        var parsedCursor = cursor is null ? null : ApiHelpers.DecodeCursor(cursor);
        if (cursor is not null && parsedCursor is null)
        {
            return InvalidCursor();
        }

        var me = ApiHelpers.CurrentUserId(context);
        var otherId = await db.Users.Where(user => user.Username == username)
            .Select(user => (Guid?)user.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (otherId is null)
        {
            return Results.NotFound();
        }
        var other = otherId.Value;

        // DateTime.UtcNow burada SQL'de now()'a çevrilir → read_at >= created_at kısıtı bozulmaz
        await db.Messages
            .Where(message => message.SenderId == other && message.RecipientId == me && message.ReadAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(
                message => message.ReadAt, message => (DateTime?)DateTime.UtcNow), cancellationToken);

        var query = db.Messages.Where(message =>
            (message.SenderId == me && message.RecipientId == other)
            || (message.SenderId == other && message.RecipientId == me));
        if (parsedCursor is not null)
        {
            query = query.Where(message => message.CreatedAt < parsedCursor.CreatedAt
                || (message.CreatedAt == parsedCursor.CreatedAt && message.Id < parsedCursor.Id));
        }

        var items = await query
            .OrderByDescending(message => message.CreatedAt)
            .ThenByDescending(message => message.Id)
            .Select(message => new MessageResponse(message.Id, message.SenderId, message.RecipientId,
                message.Content, message.CreatedAt, message.ReadAt))
            .Take(TweetPaging.PageSize + 1)
            .ToListAsync(cancellationToken);

        return Results.Ok(ToPage(items));
    }

    private static async Task<IResult> SendAsync(
        string username, SendMessageRequest request, HttpContext context, AppDbContext db,
        CancellationToken cancellationToken)
    {
        var content = request.Content?.Trim();
        var contentLength = content?.EnumerateRunes().Count() ?? 0;
        if (contentLength is < 1 or > 1000)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["content"] = ["Mesaj içeriği boş olmamalı ve 1000 karakteri aşmamalıdır."]
            });
        }

        var me = ApiHelpers.CurrentUserId(context);
        var recipientId = await db.Users.Where(user => user.Username == username)
            .Select(user => (Guid?)user.Id)
            .SingleOrDefaultAsync(cancellationToken);
        if (recipientId is null)
        {
            return Results.NotFound();
        }
        if (recipientId == me)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["username"] = ["Kendinize mesaj gönderemezsiniz."]
            });
        }

        var message = new Message { SenderId = me, RecipientId = recipientId.Value, Content = content! };
        db.Messages.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        var response = new MessageResponse(message.Id, message.SenderId, message.RecipientId,
            message.Content, message.CreatedAt, message.ReadAt);
        return Results.Created($"/api/messages/{username}", response);
    }

    private static PageResponse<MessageResponse> ToPage(List<MessageResponse> items)
    {
        var hasMore = items.Count > TweetPaging.PageSize;
        if (hasMore)
        {
            items.RemoveAt(TweetPaging.PageSize);
        }
        var nextCursor = hasMore && items.Count > 0
            ? ApiHelpers.EncodeCursor(new TweetCursor(items[^1].CreatedAt, items[^1].Id))
            : null;
        return new PageResponse<MessageResponse>(items, nextCursor);
    }

    private static IResult InvalidCursor() =>
        Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["cursor"] = ["Cursor biçimi geçersiz."]
        });
}
using Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public static class NotificationWriter
{
    // Asıl kaydı (beğeni/yanıt/takip) ekleyen işlemle aynı transaction içinde çağrılmalı.
    public static async Task AddAsync(
        AppDbContext db, Guid recipientId, Guid actorId, string type, Guid? tweetId,
        CancellationToken cancellationToken)
    {
        if (recipientId == actorId)   // kişi kendi hareketinden bildirim almaz
        {
            return;
        }

        await db.Database.ExecuteSqlInterpolatedAsync($"""
            insert into public.notifications (recipient_id, actor_id, type, tweet_id)
            values ({recipientId}, {actorId}, {type}, {tweetId})
            on conflict do nothing
            """, cancellationToken);
    }
}
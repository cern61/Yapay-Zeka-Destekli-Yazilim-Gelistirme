using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Tweet> Tweets => Set<Tweet>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    var isPostgreSql = Database.IsNpgsql();

    if (isPostgreSql)
    {
        modelBuilder.HasDefaultSchema("public");
    }
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasDefaultValueSql(isPostgreSql ? "gen_random_uuid()" : null);
            entity.Property(user => user.Username).HasColumnType(isPostgreSql ? "citext" : "TEXT")
.UseCollation(isPostgreSql ? null : "NOCASE").HasMaxLength(15);
            entity.Property(user => user.DisplayName).HasMaxLength(50);
            entity.Property(user => user.Email).HasColumnType(isPostgreSql ? "citext" : "TEXT")
.UseCollation(isPostgreSql ? null : "NOCASE").HasMaxLength(254);
            entity.Property(user => user.Bio).HasMaxLength(160);
            entity.Property(user => user.Location).HasMaxLength(30);
            entity.Property(user => user.Website).HasMaxLength(100);
            entity.Property(user => user.AvatarUrl).HasMaxLength(500);
            entity.Property(user => user.CreatedAt).HasDefaultValueSql(isPostgreSql ? "now()" : "CURRENT_TIMESTAMP");
            entity.Property(user => user.UpdatedAt).HasDefaultValueSql(isPostgreSql ? "now()" : "CURRENT_TIMESTAMP");
        });

        modelBuilder.Entity<Tweet>(entity =>
        {
            entity.HasKey(tweet => tweet.Id);
            entity.Property(tweet => tweet.Id).HasDefaultValueSql(isPostgreSql ? "gen_random_uuid()" : null);
            entity.Property(tweet => tweet.Content).HasMaxLength(280);
            entity.Property(tweet => tweet.CreatedAt).HasDefaultValueSql(isPostgreSql ? "now()" : "CURRENT_TIMESTAMP");
            entity.HasQueryFilter(tweet => tweet.DeletedAt == null);
            entity.HasOne(tweet => tweet.Author).WithMany().HasForeignKey(tweet => tweet.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Tweet>().WithMany().HasForeignKey(tweet => tweet.ParentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasKey(like => new { like.UserId, like.TweetId });
            entity.Property(like => like.CreatedAt).HasDefaultValueSql(isPostgreSql ? "now()" : "CURRENT_TIMESTAMP");
            entity.HasOne<User>().WithMany().HasForeignKey(like => like.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Tweet>().WithMany().HasForeignKey(like => like.TweetId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasKey(follow => new { follow.FollowerId, follow.FollowingId });
            entity.Property(follow => follow.CreatedAt).HasDefaultValueSql(isPostgreSql ? "now()" : "CURRENT_TIMESTAMP");
            entity.HasOne<User>().WithMany().HasForeignKey(follow => follow.FollowerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>().WithMany().HasForeignKey(follow => follow.FollowingId).OnDelete(DeleteBehavior.Cascade);
        });
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(message => message.Id);
            entity.Property(message => message.Id).HasDefaultValueSql(isPostgreSql ? "gen_random_uuid()" : null);
            entity.Property(message => message.Content).HasMaxLength(1000);
            entity.Property(message => message.CreatedAt).HasDefaultValueSql(isPostgreSql ? "now()" : "CURRENT_TIMESTAMP");
            entity.HasOne(message => message.Sender).WithMany().HasForeignKey(message => message.SenderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>().WithMany().HasForeignKey(message => message.RecipientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.Id).HasDefaultValueSql(isPostgreSql ? "gen_random_uuid()" : null);
            entity.Property(notification => notification.Type).HasMaxLength(10);
            entity.Property(notification => notification.CreatedAt).HasDefaultValueSql(isPostgreSql ? "now()" : "CURRENT_TIMESTAMP");
            entity.HasOne(notification => notification.Actor).WithMany().HasForeignKey(notification => notification.ActorId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>().WithMany().HasForeignKey(notification => notification.RecipientId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Tweet>().WithMany().HasForeignKey(notification => notification.TweetId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

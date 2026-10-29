using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Tweet> Tweets => Set<Tweet>();
    public DbSet<Like> Likes => Set<Like>();
    public DbSet<Follow> Follows => Set<Follow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(user => user.Username).HasColumnType("citext").HasMaxLength(15);
            entity.Property(user => user.DisplayName).HasMaxLength(50);
            entity.Property(user => user.Email).HasColumnType("citext").HasMaxLength(254);
            entity.Property(user => user.Bio).HasMaxLength(160);
            entity.Property(user => user.Location).HasMaxLength(30);
            entity.Property(user => user.Website).HasMaxLength(100);
            entity.Property(user => user.AvatarUrl).HasMaxLength(500);
            entity.Property(user => user.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(user => user.UpdatedAt).HasDefaultValueSql("now()");
        });

        modelBuilder.Entity<Tweet>(entity =>
        {
            entity.HasKey(tweet => tweet.Id);
            entity.Property(tweet => tweet.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(tweet => tweet.Content).HasMaxLength(280);
            entity.Property(tweet => tweet.CreatedAt).HasDefaultValueSql("now()");
            entity.HasQueryFilter(tweet => tweet.DeletedAt == null);
            entity.HasOne(tweet => tweet.Author).WithMany().HasForeignKey(tweet => tweet.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Tweet>().WithMany().HasForeignKey(tweet => tweet.ParentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Like>(entity =>
        {
            entity.HasKey(like => new { like.UserId, like.TweetId });
            entity.Property(like => like.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne<User>().WithMany().HasForeignKey(like => like.UserId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Tweet>().WithMany().HasForeignKey(like => like.TweetId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasKey(follow => new { follow.FollowerId, follow.FollowingId });
            entity.Property(follow => follow.CreatedAt).HasDefaultValueSql("now()");
            entity.HasOne<User>().WithMany().HasForeignKey(follow => follow.FollowerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<User>().WithMany().HasForeignKey(follow => follow.FollowingId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

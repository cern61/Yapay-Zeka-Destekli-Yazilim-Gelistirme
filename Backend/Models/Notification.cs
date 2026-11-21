namespace Backend.Models;

public sealed class Notification
{
    public Guid Id { get; set; }
    public Guid RecipientId { get; set; }      // bildirimi alan
    public Guid ActorId { get; set; }          // hareketi yapan
    public required string Type { get; set; }  // like / reply / follow
    public Guid? TweetId { get; set; }         // follow'da null
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public User Actor { get; set; } = null!;
}

// Veritabanındaki CHECK kısıtıyla birebir aynı değerler
public static class NotificationTypes
{
    public const string Like = "like";
    public const string Reply = "reply";
    public const string Follow = "follow";
}
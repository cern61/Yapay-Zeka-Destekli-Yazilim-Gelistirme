namespace Backend.Models;

public sealed class Like
{
    public Guid UserId { get; set; }
    public Guid TweetId { get; set; }
    public DateTime CreatedAt { get; set; }
}

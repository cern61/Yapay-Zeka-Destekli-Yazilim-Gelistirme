namespace Backend.Models;

public sealed class Tweet
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ParentId { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public User Author { get; set; } = null!;
}

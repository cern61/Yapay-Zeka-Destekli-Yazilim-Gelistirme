namespace Backend.Models;

public sealed class Message
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public Guid RecipientId { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }      // null = okunmamış
    public User Sender { get; set; } = null!;
}
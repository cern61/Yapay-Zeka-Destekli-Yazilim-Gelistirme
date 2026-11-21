namespace Backend.Contracts;

public sealed record RegisterRequest(string Username, string DisplayName, string Email, string Password);
public sealed record LoginRequest(string Login, string Password);
public sealed record UpdateProfileRequest(string DisplayName, string? Bio, string? Location, string? Website, string? AvatarUrl);
public sealed record CreateTweetRequest(string Content, Guid? ParentId);
public sealed record UserSummary(Guid Id, string Username, string DisplayName, string? AvatarUrl);
public sealed record UserProfile(Guid Id, string Username, string DisplayName, string? Bio, string? Location,
    string? Website, string? AvatarUrl, DateTime CreatedAt, int TweetCount, int FollowerCount,
    int FollowingCount, bool IsFollowing);
public sealed record TweetResponse(Guid Id, string? Content, bool IsDeleted, Guid? ParentId,
    string? ParentContent, bool ParentDeleted, DateTime CreatedAt, UserSummary Author,
    int LikeCount, int ReplyCount, bool IsLiked);
public sealed record AuthResponse(string AccessToken, DateTime ExpiresAt, UserProfile User);
public sealed record PageResponse<T>(IReadOnlyList<T> Items, string? NextCursor);
public sealed record TweetCursor(DateTime CreatedAt, Guid Id);
public sealed record SendMessageRequest(string Content);
public sealed record MessageResponse(Guid Id, Guid SenderId, Guid RecipientId, string Content,
    DateTime CreatedAt, DateTime? ReadAt);
public sealed record ConversationResponse(UserSummary User, MessageResponse LastMessage, int UnreadCount);
public sealed record NotificationResponse(Guid Id, string Type, UserSummary Actor, Guid? TweetId,
    string? TweetContent, bool TweetDeleted, DateTime CreatedAt, DateTime? ReadAt);
public sealed record CountResponse(int Count);
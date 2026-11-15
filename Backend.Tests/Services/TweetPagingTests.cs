using Backend.Contracts;
using Backend.Models;
using Backend.Services;

namespace Backend.Tests.Services;

// TweetPaging.BeforeCursor: "daha eski tweetleri getir" filtresini veritabanı olmadan,
// bellekteki bir liste üzerinde test eder.
public class TweetPagingTests
{
    private static readonly DateTime BaseTime = new(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Tweet MakeTweet(Guid id, DateTime createdAt) => new()
    {
        Id = id,
        UserId = Guid.NewGuid(),
        Content = "test",
        CreatedAt = createdAt
    };

    // Yalnızca son baytı farklı olan Guid'ler; böylece sıralamaları her ortamda aynıdır.
    private static Guid IdEndingWith(byte last) =>
        new($"00000000-0000-0000-0000-0000000000{last:x2}");

    [Fact]
    public void BeforeCursor_NullCursor_ReturnsAllTweets()
    {
        var tweets = Enumerable.Range(0, 5)
            .Select(i => MakeTweet(Guid.NewGuid(), BaseTime.AddMinutes(i)))
            .ToList();

        var result = TweetPaging.BeforeCursor(tweets.AsQueryable(), null).ToList();

        Assert.Equal(5, result.Count);
    }

    [Fact]
    public void BeforeCursor_ReturnsOnlyOlderTweets()
    {
        var older = MakeTweet(Guid.NewGuid(), BaseTime.AddMinutes(-10));
        var cursorTweet = MakeTweet(Guid.NewGuid(), BaseTime);
        var newer = MakeTweet(Guid.NewGuid(), BaseTime.AddMinutes(10));
        var tweets = new[] { older, cursorTweet, newer }.AsQueryable();

        var result = TweetPaging.BeforeCursor(tweets, new TweetCursor(cursorTweet.CreatedAt, cursorTweet.Id)).ToList();

        Assert.Single(result);
        Assert.Equal(older.Id, result[0].Id);
    }

    [Fact]
    public void BeforeCursor_DoesNotReturnCursorTweetItself()
    {
        var cursorTweet = MakeTweet(Guid.NewGuid(), BaseTime);

        var result = TweetPaging.BeforeCursor(
            new[] { cursorTweet }.AsQueryable(),
            new TweetCursor(cursorTweet.CreatedAt, cursorTweet.Id)).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void BeforeCursor_SameTimestamp_UsesIdAsTieBreaker()
    {
        // Aynı saniyede atılan tweetler: sıralama Id'ye göre devam etmeli,
        // yoksa sayfa geçişlerinde tweet kaybolur veya tekrar eder.
        var low = MakeTweet(IdEndingWith(0x01), BaseTime);
        var mid = MakeTweet(IdEndingWith(0x02), BaseTime);
        var high = MakeTweet(IdEndingWith(0x03), BaseTime);
        var tweets = new[] { low, mid, high }.AsQueryable();

        var result = TweetPaging.BeforeCursor(tweets, new TweetCursor(BaseTime, mid.Id)).ToList();

        Assert.Single(result);
        Assert.Equal(low.Id, result[0].Id);
    }

    [Fact]
    public void PageSize_Is20()
    {
        Assert.Equal(20, TweetPaging.PageSize);
    }
}

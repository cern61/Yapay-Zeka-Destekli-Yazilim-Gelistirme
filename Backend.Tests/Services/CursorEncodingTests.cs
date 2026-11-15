using Backend.Contracts;
using Backend.Services;

namespace Backend.Tests.Services;

// ApiHelpers.EncodeCursor / DecodeCursor: akış sayfalamasında kullanılan cursor'ın
// URL-güvenli base64 formatında kodlanıp geri çözülmesini test eder.
public class CursorEncodingTests
{
    [Fact]
    public void EncodeThenDecode_ReturnsSameCursor()
    {
        var original = new TweetCursor(
            new DateTime(2026, 11, 13, 10, 30, 15, DateTimeKind.Utc).AddTicks(1234567),
            Guid.NewGuid());

        var encoded = ApiHelpers.EncodeCursor(original);
        var decoded = ApiHelpers.DecodeCursor(encoded);

        Assert.NotNull(decoded);
        Assert.Equal(original.Id, decoded!.Id);
        Assert.Equal(original.CreatedAt, decoded.CreatedAt);
    }

    [Fact]
    public void Encode_ProducesUrlSafeString()
    {
        // Birçok farklı cursor üretip hiçbirinde URL'de sorun çıkaracak karakter olmadığını kontrol ediyoruz.
        for (var i = 0; i < 200; i++)
        {
            var cursor = new TweetCursor(DateTime.UtcNow.AddMinutes(-i), Guid.NewGuid());

            var encoded = ApiHelpers.EncodeCursor(cursor);

            Assert.DoesNotContain('+', encoded);
            Assert.DoesNotContain('/', encoded);
            Assert.DoesNotContain('=', encoded);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Decode_NullOrEmpty_ReturnsNull(string? value)
    {
        Assert.Null(ApiHelpers.DecodeCursor(value));
    }

    [Fact]
    public void Decode_TooLongValue_ReturnsNull()
    {
        var tooLong = new string('a', 513);

        Assert.Null(ApiHelpers.DecodeCursor(tooLong));
    }

    [Theory]
    [InlineData("bu-gecerli-bir-cursor-degil!!")]
    [InlineData("%%%")]
    [InlineData("YWJj")] // "abc" -> geçerli base64 ama geçerli JSON değil
    public void Decode_GarbageInput_ReturnsNullInsteadOfThrowing(string value)
    {
        var exception = Record.Exception(() => ApiHelpers.DecodeCursor(value));

        Assert.Null(exception);
        Assert.Null(ApiHelpers.DecodeCursor(value));
    }

    [Fact]
    public void Decode_EmptyGuid_ReturnsNull()
    {
        var encoded = ApiHelpers.EncodeCursor(new TweetCursor(DateTime.UtcNow, Guid.Empty));

        Assert.Null(ApiHelpers.DecodeCursor(encoded));
    }

    [Fact]
    public void Decode_DefaultDate_ReturnsNull()
    {
        var encoded = ApiHelpers.EncodeCursor(new TweetCursor(default, Guid.NewGuid()));

        Assert.Null(ApiHelpers.DecodeCursor(encoded));
    }
}

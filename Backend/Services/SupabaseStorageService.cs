
using System.Net.Http.Headers;

namespace Backend.Services;

public sealed class SupabaseStorageService
{
    private const string BucketName = "avatars";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;

    public SupabaseStorageService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
    }

    public async Task<string> UploadAvatarAsync(
        Guid userId,
        Stream fileStream,
        string contentType,
        string extension,
        CancellationToken cancellationToken)
    {
        var supabaseUrl = _configuration["Supabase:Url"]?.TrimEnd('/');
        var secretKey = _configuration["Supabase:SecretKey"];

        if (!Uri.TryCreate(supabaseUrl, UriKind.Absolute, out var baseUri)
            || baseUri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidOperationException(
                "Supabase URL yapılandırması geçersiz.");
        }

        if (string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                "Supabase SecretKey yapılandırılmamış.");
        }

        // Her fotoğraf için benzersiz dosya yolu oluştur.
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var objectPath = $"{userId:N}/{fileName}";

        var uploadUrl =
            $"{supabaseUrl}/storage/v1/object/{BucketName}/{objectPath}";

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            uploadUrl);

        request.Headers.TryAddWithoutValidation("apikey", secretKey);

        request.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", secretKey);

        request.Headers.TryAddWithoutValidation("x-upsert", "false");

        request.Content = new StreamContent(fileStream);
        request.Content.Headers.ContentType =
            new MediaTypeHeaderValue(contentType);

        var client = _httpClientFactory.CreateClient();

        using var response = await client.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(
                cancellationToken);

            throw new HttpRequestException(
                $"Supabase Storage yüklemesi başarısız. " +
                $"HTTP {(int)response.StatusCode}: {error}",
                null,
                response.StatusCode);
        }

        // Public bucket için fotoğrafın erişim adresini oluştur.
        return $"{supabaseUrl}/storage/v1/object/public/{BucketName}/{objectPath}";
    }
}

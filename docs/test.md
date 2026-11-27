# Test

Sorumlu: Yusuf Ali Başer · xUnit · ASP.NET Core `WebApplicationFactory` · SQLite (bellek içi)

## Çalıştırma

Depo kök klasöründen:

```bash
dotnet test Backend.Tests
```

Testler için Supabase bağlantısı, `user-secrets` veya internet gerekmez. Gerekli ayarlar test kodunun içinde sahte değerlerle verilir.

## Nasıl çalışır?

Testler iki türdür:

| Tür | Nasıl | Veritabanı |
|-----|-------|------------|
| **Birim testleri** | Bir sınıfı (ör. `JwtTokenService`) doğrudan çağırır. | Yok |
| **Uç nokta (entegrasyon) testleri** | `WebApplicationFactory` uygulamayı bellekte başlatır, testler gerçek HTTP istekleri gönderir ve durum kodunu/yanıt gövdesini kontrol eder. | Senaryoya göre |

Uç nokta testlerinde iki ayrı test ortamı (factory) kullanılır:

- **`ApiTestFactory`** (`Backend.Tests/Infrastructure/`): Veritabanına hiç ulaşmadan cevaplanan senaryolar içindir: yetki kontrolü, girdi doğrulama, bozuk cursor. Bağlantı dizesi bilerek ulaşılamayan bir adrese işaret eder; bir test yanlışlıkla veritabanına giderse hemen fark edilir. `CreateValidToken()` ve `CreateCustomToken()` ile geçerli, sahte veya süresi dolmuş token üretir.
- **`AuthTestFactory`** (`Endpoints/AuthEndpointsTests.cs`): Veritabanı gerektiren başarılı akışlar içindir. Her test bellekte yeni ve boş bir SQLite veritabanı açar (`EnsureCreated`), test bitince veritabanı silinir. Testler birbirinin verisini görmez.

Ayarlar `UseSetting` ile verilir; `Program.cs` `Jwt:Secret` değerini uygulama kurulurken okuduğu için `ConfigureAppConfiguration` ile verilen değerler oraya geç ulaşır.

## Test dosyaları

| Dosya | Kapsam |
|-------|--------|
| `Services/CursorEncodingTests.cs` | Cursor'ın URL-güvenli kodlanması ve geri çözülmesi; bozuk, boş, çok uzun girdide `null` dönmesi |
| `Services/TweetPagingTests.cs` | "Daha eski tweetler" filtresi; aynı zamanlı tweetlerde `Id` ile sıralama |
| `Services/JwtTokenServiceTests.cs` | Token içeriği (`sub`, kullanıcı adı), issuer/audience, 30 dk süre, yanlış anahtarla doğrulanamama |
| `Endpoints/AuthEndpointsTests.cs` | Kayıt/giriş girdi doğrulaması |
| `Endpoints/AuthFlowTests.cs` | Kayıt → token → korumalı uç nokta; giriş (kullanıcı adı/e-posta); tekrarlanan kullanıcı adı/e-posta (büyük/küçük harf duyarsız); şifrenin BCrypt ile saklanması ve yanıtta dönmemesi; yanlış şifre ve olmayan kullanıcı için `401` |
| `Endpoints/UserEndpointsTests.cs` | Olmayan profil `404`, tokensız profil güncelleme `401` |
| `Endpoints/ProfileAndFollowEndpointsTests.cs` | Tokensız takip/takipten çıkma; görünen ad, bio, konum uzunlukları; web sitesi ve avatar için yalnızca `http/https` (`javascript:` ve `data:` adresleri reddedilir); birden fazla hatanın birlikte bildirilmesi |
| `Endpoints/TweetEndpointsTests.cs` | Tokensız, sahte ve süresi dolmuş token; tweet içeriği 1-280 karakter (boşluk, `null`, 281 karakter, 281 emoji); geçersiz id biçimi |
| `Endpoints/FeedEndpointsTests.cs` | Tokensız kişisel akış; genel ve kişisel akışta bozuk cursor |
| `Endpoints/MessageEndpointsTests.cs` | Tokensız mesaj işlemleri; mesaj içeriği 1-1000 karakter; bozuk cursor |
| `Endpoints/NotificationEndpointsTests.cs` | Tokensız bildirim işlemleri; bozuk cursor; geçersiz bildirim id'si |

## Henüz test edilmeyenler

| Konu | Neden |
|------|-------|
| Beğeni, yanıt ve takip sonrası bildirim oluşması | `NotificationWriter` bildirimi PostgreSQL'e özgü ham SQL ile (`public.notifications`, snake_case sütunlar) yazar. SQLite test veritabanında bu tablo adı bulunmadığından bu akışlar testte çalışmaz. Bildirim yazımı EF Core ile yapılırsa veya test veritabanı aynı adlandırmayı kullanırsa testleri eklenebilir. |
| Tweet, takip ve mesajlaşmanın başarılı akışları | Yukarıdaki bildirim konusuna bağlıdır; bildirim üretmeyen kısımlar (tweet atma, görüntüleme, silme) sıradaki iştir. |
| Frontend | Frontend `main` dalına birleştirildikten sonra manuel test senaryoları ve servis testleri eklenecektir. |

## Yapay zeka kullanımı

Testlerin taslakları Claude ile hazırlandı. Her test dosyası incelendi, Codespace'te `dotnet test` ile çalıştırıldı ve GitHub'a pull request ile gönderildi. Örneğin ilk denemede `Jwt:Secret` hatasıyla açılmayan uç nokta testleri, ayarların `ConfigureAppConfiguration` yerine `UseSetting` ile verilmesiyle düzeltildi.

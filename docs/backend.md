# Backend API

ASP.NET Core 8 minimal API; Supabase PostgreSQL'e EF Core/Npgsql ile bağlanır. Veritabanı tablolarını oluşturmaz veya migration çalıştırmaz. Önce [`database/schema.sql`](../database/schema.sql) Supabase'de uygulanmış olmalıdır.

## Yerel kurulum

PowerShell'de depo kök klasöründeyken:

```powershell
dotnet restore Backend\Backend.csproj
dotnet user-secrets --project Backend\Backend.csproj set "ConnectionStrings:Default" "Host=<session-pooler-host>;Port=5432;Database=postgres;Username=postgres.<project-ref>;Password=<database-password>;SSL Mode=Require;Trust Server Certificate=true;Maximum Pool Size=3"
dotnet user-secrets --project Backend\Backend.csproj set "Jwt:Secret" "<at-least-32-random-bytes>"
dotnet run --project Backend\Backend.csproj
```

Bağlantı dizesinde Supabase **Session pooler** bilgilerini kullanın. Gerçek veritabanı şifresini veya JWT sırrını repoya, `appsettings.json` dosyasına ya da yapay zeka sohbetlerine koymayın. JWT sırrı en az 32 bayt olmalı ve tahmin edilmesi zor seçilmelidir. Değerler `user-secrets` ile yerel makinede saklanır.

Geliştirme ortamında Swagger UI `/swagger` adresindedir; `/health` veritabanına bağlanmadan API durumunu döndürür. Frontend için yerel CORS izinleri `localhost:3000` ve `localhost:5173`'tür. Dağıtımda `Cors:AllowedOrigins` yapılandırma dizisini gerçek frontend adresleriyle ayarlayın.

## Uçlar

| Yöntem | Yol | Kimlik doğrulama | Açıklama |
|---|---|---|---|
| `POST` | `/api/auth/register` | Hayır | Kullanıcı oluşturur ve JWT verir |
| `POST` | `/api/auth/login` | Hayır | Kullanıcı adı/e-posta ve şifreyle giriş |
| `GET` | `/api/users/{username}` | Hayır | Profil ve sayaçlar |
| `PUT` | `/api/users/me` | Evet | Görünen ad ve profil alanlarını günceller |
| `GET` | `/api/users/{username}/tweets` | Hayır | Profil tweetleri ve yanıtları |
| `GET` | `/api/users/{username}/followers` | Hayır | En fazla 100 takipçi |
| `GET` | `/api/users/{username}/following` | Hayır | En fazla 100 takip edilen |
| `POST` / `DELETE` | `/api/users/{username}/follow` | Evet | Takip et / takipten çık |
| `GET` | `/api/tweets/{id}` | Hayır | Tweet veya silinmiş tweet yer tutucusu |
| `POST` | `/api/tweets` | Evet | Tweet veya yanıt oluşturur (`content`, isteğe bağlı `parentId`) |
| `DELETE` | `/api/tweets/{id}` | Evet | Yalnızca kendi tweetini soft delete eder |
| `GET` | `/api/tweets/{id}/replies` | Hayır | Yanıtları listeler |
| `PUT` / `DELETE` | `/api/tweets/{id}/like` | Evet | Beğen / beğeniyi kaldır |
| `GET` | `/api/feed/public` | Hayır | Genel akış |
| `GET` | `/api/feed/home` | Evet | Kendi tweetleri ve takip edilenlerin akışı |

Kimlik doğrulamalı istekler `Authorization: Bearer <token>` başlığı kullanır. Akış ve tweet listeleri `(created_at, id)` sıralı cursor ile 20 kayıt döndürür; devam etmek için yanıtın `nextCursor` değerini `?cursor=` parametresine verin. Son sayfada `nextCursor` `null` olur.

## Davranış ve güvenlik

- Şifreler BCrypt ile hash'lenir; ham şifre veya hash yanıt gövdesinde dönmez.
- Kullanıcı adı ve e-posta PostgreSQL `citext` eşlemesiyle harf duyarsız aranır.
- Tweet, profil, takip ve beğeni doğrulamaları API'de yapılır; veritabanı kısıtları da korunur.
- Silinen tweetler içerik ve silinme durumu ile yer tutucu olarak dönebilir; akışlarda listelenmez. Silinmiş tweetlere yanıt/beğeni oluşturulamaz.
- Veritabanı hataları genel hata yanıtlarına çevrilir; ayrıntılar yalnızca sunucu loglarına yazılır.

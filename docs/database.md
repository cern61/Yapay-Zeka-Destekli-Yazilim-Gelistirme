# Veritabanı

Sorumlu: Ahmet Yasin Geldi · Supabase (PostgreSQL) · Şema: [`database/schema.sql`](../database/schema.sql) · Diyagram: [`database/schema.dbml`](../database/schema.dbml) ([dbdiagram.io](https://dbdiagram.io)'ya yapıştırılır)

## Dosyalar

| Dosya | Ne işe yarar |
|-------|--------------|
| `database/schema.sql` | Şemanın **tek doğru kaynağı**. Boş veritabanında bir kez çalıştırılır. |
| `database/seed.sql` | İsteğe bağlı örnek veri: 15 kullanıcı, 145 tweet, takip ve beğeniler. Tüm şifreler `Test1234`. Yalnızca boş veritabanına yüklenir. |
| `database/seed_mesaj_bildirim.sql` | İsteğe bağlı örnek mesaj ve bildirim verisi. `seed.sql`'den sonra çalıştırılır. Bildirimler mevcut beğeni, yanıt ve takiplerden türetilir. |
| `database/changes/` | Kurulu veritabanına sonradan uygulanan şema değişiklikleri. Her dosya bir kez, numara sırasıyla çalıştırılır. Aynı değişiklik `schema.sql`'e de işlenir, bu yüzden yeni kurulumda bu klasör çalıştırılmaz. |
| `database/schema.dbml` | Yalnızca çizim içindir. Supabase'de **çalışmaz**. |

## Kurulum sırası

- **Yeni (boş) veritabanı:** `schema.sql` → `seed.sql` → `seed_mesaj_bildirim.sql`
- **Mevcut ortak veritabanı:** `changes/` içindeki henüz uygulanmamış dosyalar → `seed_mesaj_bildirim.sql`

| Değişiklik | Ne ekler | Ortak veritabanında |
|------------|----------|---------------------|
| `001_mesajlar_bildirimler.sql` | `messages`, `notifications` tabloları | uygulanacak |

## Tablolar

- **users**: `username` ve `email` büyük/küçük harf duyarsız ve benzersizdir (`citext`). Kullanıcı adı `^[A-Za-z0-9_]{3,15}$` kuralına uyar. `password_hash` yalnızca BCrypt hash kabul eder.
- **tweets**: Tweetler ve yanıtlar aynı tablodadır. `parent_id` doluysa kayıt yanıttır, yanıta da yanıt verilebilir. İçerik 1-280 karakterdir.
- **likes**: Birincil anahtar `(user_id, tweet_id)`, aynı tweet iki kez beğenilemez.
- **follows**: Birincil anahtar `(follower_id, following_id)`, kişi kendini takip edemez.
- **messages**: Özel mesajlar (`sender_id` → `recipient_id`). Ayrı sohbet tablosu yoktur, iki kişi arasındaki mesajların tamamı bir sohbettir. İçerik 1-1000 karakter, kişi kendine mesaj atamaz. `read_at` NULL ise okunmamıştır.
- **notifications**: `actor_id` kullanıcısının `recipient_id` kullanıcısına yaptığı hareket. `type` değerleri: `like` (`tweet_id` = beğenilen tweet), `reply` (`tweet_id` = yanıt tweeti), `follow` (`tweet_id` NULL). Kişi kendi hareketinden bildirim almaz, aynı hareket için ikinci bildirim oluşmaz. `read_at` NULL ise okunmamıştır.

## Bağlantı

Bağlantı için Supabase'in **Session pooler** adresi kullanılır (port 5432). Direct connection IPv6 gerektirir, transaction pooler ise Npgsql ile sorun çıkarır.

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=<host>;Port=5432;Database=postgres;Username=postgres.<proje-ref>;Password=<şifre>;SSL Mode=Require;Trust Server Certificate=true;Maximum Pool Size=3"
```

- Host ve şifre Ahmet Yasin'den birebir mesajla istenir. Bu bilgiler **repoya, PR'a, issue'ya ve yapay zekaya yapıştırılmaz.**
- `Maximum Pool Size=3` ayarı kaldırılmamalı. Ücretsiz planda toplam bağlantı sayısı sınırlıdır ve herkes aynı veritabanını kullanıyor.
- Supabase API anahtarları (anon, service_role) **kullanılmaz.** Frontend veritabanına hiç bağlanmaz.

## Backend (EF Core) kuralları

```csharp
// Paketler: Npgsql.EntityFrameworkCore.PostgreSQL 8.x, EFCore.NamingConventions 8.x
options.UseNpgsql(connStr).UseSnakeCaseNamingConvention();   // CreatedAt <-> created_at

modelBuilder.Entity<User>(e =>
{
    e.Property(u => u.Username).HasColumnType("citext");   // ZORUNLU, aşağıdaki uyarıya bakın
    e.Property(u => u.Email).HasColumnType("citext");
});
modelBuilder.Entity<Tweet>().HasQueryFilter(t => t.DeletedAt == null);   // soft delete
modelBuilder.Entity<Like>().HasKey(l => new { l.UserId, l.TweetId });
modelBuilder.Entity<Follow>().HasKey(f => new { f.FollowerId, f.FollowingId });
```

- **Migration yok.** `dotnet ef migrations` / `database update` kullanılmaz. Şema değişikliği gerekiyorsa Ahmet Yasin'e iletilir ve `schema.sql` güncellenir.
- **citext uyarısı:** `HasColumnType("citext")` unutulursa parametre `text` olarak gider ve arama harfe duyarlı olur. Sonuç: `Ahmet@Example.com` ile kayıt olan kişi `ahmet@example.com` ile giriş yapamaz. Benzersizlik kuralı yine çalıştığı için bu hata kolay fark edilmez.
- **Soft delete:** Tweet silmek için `DeletedAt = DateTime.UtcNow` ve `Content = null` birlikte atanır. Biri eksik kalırsa veritabanı kaydı reddeder. Silinmiş tweete yanıt veya beğeni API'de engellenmelidir. Silinmiş tweete yazılmış eski yanıtlar listede kalır, arayüz bunlar için "yanıtlanan tweet silindi" gösterir.
- **Sayaçlar saklanmaz.** Beğeni, yanıt, takipçi ve takip sayıları `Count()` ile hesaplanır. Yanıt sayısına silinmiş yanıtlar dahil edilmez. Okunmamış mesaj ve bildirim sayıları da `read_at == null` ile sayılır.
- **Bildirimler:** Veritabanında trigger yoktur, bildirimi backend oluşturur. Beğeni, yanıt ve takip kaydıyla aynı `SaveChanges` içinde eklenir; kişi kendi tweetini beğenirse veya yanıtlarsa bildirim eklenmez. Aynı hareket tekrar edilirse (beğen-geri al-beğen) `notifications_unique_event` kısıtı ikinci kaydı reddeder; bu `23505` hatası 409'a çevrilmemeli, sessizce yok sayılmalıdır. Beğeni veya takip geri alınınca bildirim silinmez.
- **Mesajlar:** Sohbet sorgusu `(SenderId == a && RecipientId == b) || (SenderId == b && RecipientId == a)` şeklinde yazılır, mesajlar da tweetler gibi `(created_at, id)` cursor'ı ile sayfalanır. Sohbet açılınca karşı tarafın gönderdiği okunmamış mesajların `ReadAt` alanı doldurulur.
- **Zaman:** Tüm tarihler `timestamptz` tipindedir. C# tarafında `DateTime.UtcNow` kullanılır.
- **Hatalar:** Veritabanı ihlallerini API kodlarına çevirin:

  | Postgres kodu | Anlamı | API kodu |
  |---------------|--------|----------|
  | `23505` | unique ihlali | `409` |
  | `23514` | check ihlali | `400` |
  | `23503` | olmayan kayda referans | `404` |

## Akış ve sayfalama

Akışlar **cursor** ile 20'şerli sayfalanır (`?page=` kullanılmaz). İstemci son gördüğü tweetin `createdAt` ve `id` değerlerini gönderir:

```sql
where deleted_at is null and (created_at, id) < (@cursorCreatedAt, @cursorId)
order by created_at desc, id desc
limit 20
```

- **Takip akışı:** Benim tweetlerim ve takip ettiklerimin tweetleri (fan-out on read).
- **Yanıtlar:** Akışlarda ve profilde de görünür.
- **Sıralama:** `id` ikincil sıralama olarak zorunludur. Aynı anda atılmış iki tweet olursa sıra karışmasın diye.

## Test notları

- EF Core InMemory şu kuralları **çalıştırmaz:** `CHECK`, `citext`, trigger, foreign key. Kuralların hepsi API'de de doğrulanmalı ve testler API davranışını sınamalıdır.
- Harf duyarsız giriş, veritabanı kısıtları ve sayfalama gerçek Supabase üzerinde elle test edilir. Seed verisinde bunun için hazır durumlar var:
  - Boş profil: `yeni_uye`
  - Silinmiş tweete yanıtlar
  - `<script>` içeren tweet (XSS testi)
  - 272 karakterlik uzun tweet
  - `ayse_kaya`: 4 sohbet, 2'sinde okunmamış mesaj (rozet testi)
  - `<script>` içeren mesaj (`cem_ozturk` → `ayse_kaya`)
  - Mesajı olmayan kullanıcı: `yeni_uye`
- **Tek ortak veritabanı:** Elle test ederken başkalarının verisini silmeyin, kendi test kullanıcınızı açın.

## Yapay zeka ajanları için notlar

- Şemanın kaynağı `database/schema.sql` dosyasıdır. **EF migration üretme**, `Database.EnsureCreated()` veya `Migrate()` çağırma.
- Tablo ve sütun adları `snake_case`, C# tarafı `PascalCase` kullanır. Eşlemeyi `UseSnakeCaseNamingConvention()` yapar, `[Column]` / `ToTable` ile elle ad yazma.
- Kimlikler `uuid` (C#: `Guid`), varsayılan değerleri veritabanı üretir.
- `Username` / `Email` için `citext` eşlemesini kaldırma. `ToLower()` ile karşılaştırma yazma, `citext` bu işi zaten yapıyor.
- Tweet sorgularında `IgnoreQueryFilters()` yalnızca silinmiş tweetin yer tutucusu gerekiyorsa kullanılır.
- Sayaç sütunu (`like_count` vb.) ekleme önerme. Bu bir tasarım kararı (README → Veri Modeli).
- Bağlantı dizesi, şifre veya host'u koda, `appsettings.json`'a ya da örnek çıktılara yazma. Yalnızca `user-secrets` / ortam değişkeni kullanılır.
- RLS açıktır ve politika tanımlı değildir. Supabase REST/anon erişimi için politika **ekleme**.

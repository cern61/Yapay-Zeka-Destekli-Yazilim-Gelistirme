-- =============================================================================
-- Mavi Takım - Twitter Klonu veritabanı şeması
-- Hedef: Supabase (PostgreSQL 15+)
--
-- Bu dosya şemanın tek doğru kaynağıdır. EF Core tablo oluşturmaz, yalnızca
-- bu tablolara eşlenir. Boş bir veritabanında, Supabase SQL Editor'de bir kez
-- çalıştırılır. Tümü tek transaction içindedir: bir hata olursa hiçbir şey
-- oluşturulmaz.
--
-- Kurallar iki katmanda korunur: API doğrular, veritabanı da CHECK / UNIQUE /
-- FOREIGN KEY ile ikinci bir güvence sağlar. (EF Core InMemory testleri bu
-- kısıtları çalıştırmaz, bu yüzden API tarafındaki doğrulama da zorunludur.)
-- =============================================================================

begin;

-- Büyük/küçük harf duyarsız metin tipi (kullanıcı adı ve e-posta için).
-- Supabase eklentileri "extensions" şemasında tutar.
--
-- DİKKAT (backend): Harf duyarsız karşılaştırma yalnızca karşılaştırılan değer de
-- citext ise çalışır. Parametre düz text giderse arama harfe duyarlı olur ve
-- "Ahmet@Example.com" ile kayıtlı kişi "ahmet@example.com" ile giriş yapamaz.
-- EF Core'da Username ve Email için mutlaka .HasColumnType("citext") kullanılmalıdır.
create schema if not exists extensions;
create extension if not exists citext with schema extensions;


-- -----------------------------------------------------------------------------
-- users
-- -----------------------------------------------------------------------------
create table public.users (
  id            uuid primary key default gen_random_uuid(),
  username      extensions.citext not null,
  display_name  varchar(50)  not null,
  email         extensions.citext not null,
  password_hash text         not null,
  bio           varchar(160),
  location      varchar(30),
  website       varchar(100),
  avatar_url    varchar(500),
  created_at    timestamptz  not null default now(),
  updated_at    timestamptz  not null default now(),

  -- citext sayesinde 'Ahmet' ile 'ahmet' aynı kabul edilir
  constraint users_username_key unique (username),
  constraint users_email_key    unique (email),

  -- URL'de kullanıldığı için yalnızca İngilizce harf, rakam ve alt çizgi
  constraint users_username_format check (username ~ '^[A-Za-z0-9_]{3,15}$'),
  -- Yalnızca boşluktan oluşamaz
  constraint users_display_name_not_blank check (display_name ~ '\S'),
  constraint users_email_format check (
    char_length(email) <= 254 and email ~ '^[^@\s]+@[^@\s]+\.[^@\s]+$'
  ),
  -- Düz şifrenin yanlışlıkla kaydedilmesini engeller: yalnızca BCrypt hash'i kabul edilir
  constraint users_password_hash_bcrypt check (password_hash ~ '^\$2[aby]\$\d{2}\$.{53}$'),
  -- javascript: gibi zararlı bağlantıları engeller
  constraint users_website_http    check (website    ~* '^https?://'),
  constraint users_avatar_url_http check (avatar_url ~* '^https?://')
);

comment on table public.users is 'Kullanıcılar. Kullanıcı adı ve e-posta büyük/küçük harf duyarsız benzersizdir.';


-- -----------------------------------------------------------------------------
-- tweets: hem tweetler hem yanıtlar
-- parent_id doluysa kayıt bir yanıttır. Yanıta da yanıt verilebilir.
--
-- Soft delete: tweet silinince satır kalır (başkalarının yanıtları kaybolmasın
-- diye), deleted_at doldurulur ve içerik silinir:
--   update tweets set deleted_at = now(), content = null where id = ...;
-- -----------------------------------------------------------------------------
create table public.tweets (
  id         uuid primary key default gen_random_uuid(),
  user_id    uuid not null references public.users (id) on delete cascade,
  parent_id  uuid references public.tweets (id) on delete cascade,
  content    varchar(280),
  created_at timestamptz not null default now(),
  deleted_at timestamptz,

  constraint tweets_not_own_parent check (parent_id <> id),
  -- Canlı tweet: içerik dolu ve yalnızca boşluk değil (en fazla 280, varchar ile).
  -- Silinmiş tweet: içerik NULL.
  constraint tweets_content_state check (
    (deleted_at is null and content is not null and content ~ '\S')
    or (deleted_at is not null and content is null)
  )
);

comment on table public.tweets is 'Tweetler ve yanıtlar. parent_id doluysa yanıttır. deleted_at doluysa silinmiştir (soft delete).';


-- -----------------------------------------------------------------------------
-- likes: aynı kullanıcı aynı tweeti bir kez beğenebilir (birincil anahtar)
-- -----------------------------------------------------------------------------
create table public.likes (
  user_id    uuid not null references public.users  (id) on delete cascade,
  tweet_id   uuid not null references public.tweets (id) on delete cascade,
  created_at timestamptz not null default now(),

  primary key (user_id, tweet_id)
);

comment on table public.likes is 'Beğeniler. (user_id, tweet_id) birincil anahtar olduğu için aynı tweet iki kez beğenilemez.';


-- -----------------------------------------------------------------------------
-- follows: follower_id, following_id kullanıcısını takip eder
-- -----------------------------------------------------------------------------
create table public.follows (
  follower_id  uuid not null references public.users (id) on delete cascade,
  following_id uuid not null references public.users (id) on delete cascade,
  created_at   timestamptz not null default now(),

  primary key (follower_id, following_id),
  constraint follows_not_self check (follower_id <> following_id)
);

comment on table public.follows is 'Takip ilişkileri. follower_id takip eden, following_id takip edilendir. Kişi kendini takip edemez.';


-- -----------------------------------------------------------------------------
-- İndeksler
-- Akışlar cursor ile sayfalanır:
--   where (created_at, id) < (@cursorTarih, @cursorId)
--   order by created_at desc, id desc limit 20
-- Bu yüzden indeksler (created_at desc, id desc) sırasını izler.
-- -----------------------------------------------------------------------------

-- Genel akış. Yalnızca silinmemiş tweetleri tutar (kısmi indeks).
create index ix_tweets_live_created
  on public.tweets (created_at desc, id desc)
  where deleted_at is null;

-- Profil sayfası ve "takip ettiklerim" akışı. user_id yabancı anahtarını da karşılar.
create index ix_tweets_user_created
  on public.tweets (user_id, created_at desc, id desc);

-- Bir tweetin yanıtları ve yanıt sayısı. parent_id yabancı anahtarını da karşılar.
create index ix_tweets_parent_created
  on public.tweets (parent_id, created_at)
  where parent_id is not null;

-- Bir tweetin beğeni sayısı. (user_id tarafını birincil anahtar karşılar.)
create index ix_likes_tweet on public.likes (tweet_id);

-- Bir kullanıcının takipçi sayısı ve listesi. (follower_id tarafını birincil anahtar karşılar.)
create index ix_follows_following on public.follows (following_id);


-- -----------------------------------------------------------------------------
-- users.updated_at her güncellemede otomatik yenilenir
-- -----------------------------------------------------------------------------
create function public.set_updated_at()
returns trigger
language plpgsql
set search_path = ''
as $$
begin
  new.updated_at := now();
  return new;
end;
$$;

create trigger trg_users_updated_at
  before update on public.users
  for each row execute function public.set_updated_at();


-- -----------------------------------------------------------------------------
-- Güvenlik
-- Veriye yalnızca backend erişir. RLS açık ve politika tanımlı değil; ayrıca
-- Supabase'in otomatik REST API rolleri (anon, authenticated) yetkisizdir.
-- Backend "postgres" kullanıcısıyla bağlandığı için RLS'ten etkilenmez.
-- -----------------------------------------------------------------------------
alter table public.users   enable row level security;
alter table public.tweets  enable row level security;
alter table public.likes   enable row level security;
alter table public.follows enable row level security;

revoke all on public.users, public.tweets, public.likes, public.follows from anon, authenticated;
revoke execute on function public.set_updated_at() from public, anon, authenticated;

commit;

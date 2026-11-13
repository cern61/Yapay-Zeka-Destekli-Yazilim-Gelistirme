-- =============================================================================
-- Değişiklik 001: mesajlar ve bildirimler tabloları
--
-- NE İÇİN: Arayüzdeki "Mesajlar" ve "Bildirimler" bölümlerinin veri tablosu.
--
-- NASIL: Şema zaten kurulu olan (schema.sql daha önce çalıştırılmış) ortak
-- veritabanında, Supabase SQL Editor'de bir kez çalıştırılır. Yeni kurulumlarda
-- gerekmez, çünkü aynı tablolar schema.sql içinde de vardır.
-- Tek transaction içindedir: bir hata olursa hiçbir şey oluşturulmaz.
-- Tablolar zaten varsa hata verir (iki kez çalıştırılmasın diye).
-- =============================================================================

begin;

-- -----------------------------------------------------------------------------
-- messages: iki kullanıcı arasındaki özel mesajlar
-- Sohbet ayrı bir tabloda tutulmaz; iki kişi arasındaki mesajların tamamı
-- bir sohbettir:
--   where (sender_id = @a and recipient_id = @b) or (sender_id = @b and recipient_id = @a)
-- read_at NULL ise alıcı mesajı henüz okumamıştır.
-- -----------------------------------------------------------------------------
create table public.messages (
  id           uuid primary key default gen_random_uuid(),
  sender_id    uuid not null references public.users (id) on delete cascade,
  recipient_id uuid not null references public.users (id) on delete cascade,
  content      varchar(1000) not null,
  created_at   timestamptz not null default now(),
  read_at      timestamptz,

  constraint messages_not_self check (sender_id <> recipient_id),
  -- Yalnızca boşluktan oluşamaz
  constraint messages_content_not_blank check (content ~ '\S'),
  constraint messages_read_after_created check (read_at is null or read_at >= created_at)
);

comment on table public.messages is 'Özel mesajlar. İki kullanıcı arasındaki mesajların tamamı bir sohbettir. read_at NULL ise okunmamıştır.';


-- -----------------------------------------------------------------------------
-- notifications: bir kullanıcının başka bir kullanıcının hareketinden haberi
--   like   : actor_id, recipient_id'nin tweetini beğendi   (tweet_id = beğenilen tweet)
--   reply  : actor_id, recipient_id'nin tweetine yanıt verdi (tweet_id = yanıt tweeti)
--   follow : actor_id, recipient_id'yi takip etti           (tweet_id = NULL)
-- Bildirimi backend oluşturur (beğeni, yanıt, takip işlemiyle aynı anda).
-- Kişi kendi hareketinden bildirim almaz.
-- -----------------------------------------------------------------------------
create table public.notifications (
  id           uuid primary key default gen_random_uuid(),
  recipient_id uuid not null references public.users  (id) on delete cascade,
  actor_id     uuid not null references public.users  (id) on delete cascade,
  type         varchar(10) not null,
  tweet_id     uuid references public.tweets (id) on delete cascade,
  created_at   timestamptz not null default now(),
  read_at      timestamptz,

  constraint notifications_type_valid check (type in ('like', 'reply', 'follow')),
  -- Beğeni ve yanıtta tweet zorunlu, takipte tweet olmaz
  constraint notifications_tweet_by_type check (
    (type = 'follow' and tweet_id is null)
    or (type in ('like', 'reply') and tweet_id is not null)
  ),
  constraint notifications_not_self check (recipient_id <> actor_id),
  constraint notifications_read_after_created check (read_at is null or read_at >= created_at),
  -- Aynı hareket için ikinci bildirim oluşmaz (beğen-geri al-beğen, takip et-bırak-takip et).
  -- NULLS NOT DISTINCT: takip bildirimlerinde tweet_id NULL olduğu halde tekrar engellenir.
  -- Backend eklerken "on conflict do nothing" kullanmalıdır.
  constraint notifications_unique_event unique nulls not distinct (recipient_id, actor_id, type, tweet_id)
);

comment on table public.notifications is 'Bildirimler: beğeni, yanıt ve takip. read_at NULL ise okunmamıştır.';


-- -----------------------------------------------------------------------------
-- İndeksler
-- -----------------------------------------------------------------------------

-- Bir sohbetin mesajları (iki yönün her biri bu indeksten okunur).
-- sender_id yabancı anahtarını da karşılar.
create index ix_messages_pair_created
  on public.messages (sender_id, recipient_id, created_at desc, id desc);

-- Gelen kutusu, sohbet listesi ve okunmamış mesaj sayısı.
-- recipient_id yabancı anahtarını da karşılar.
create index ix_messages_recipient_created
  on public.messages (recipient_id, created_at desc, id desc);

-- Bildirim listesi, cursor ile sayfalanır. recipient_id yabancı anahtarını da karşılar.
create index ix_notifications_recipient_created
  on public.notifications (recipient_id, created_at desc, id desc);

-- Okunmamış bildirim sayısı (rozet). Yalnızca okunmamışları tutar.
create index ix_notifications_recipient_unread
  on public.notifications (recipient_id)
  where read_at is null;

-- Yabancı anahtarlar: kullanıcı ya da tweet silinince cascade hızlı çalışsın.
-- (recipient_id, actor_id, type, tweet_id) unique indeksi actor_id'yi karşılamaz.
create index ix_notifications_actor on public.notifications (actor_id);
create index ix_notifications_tweet on public.notifications (tweet_id) where tweet_id is not null;


-- -----------------------------------------------------------------------------
-- Güvenlik: diğer tablolarla aynı. RLS açık, politika yok, REST rolleri yetkisiz.
-- -----------------------------------------------------------------------------
alter table public.messages      enable row level security;
alter table public.notifications enable row level security;

revoke all on public.messages, public.notifications from anon, authenticated;

commit;

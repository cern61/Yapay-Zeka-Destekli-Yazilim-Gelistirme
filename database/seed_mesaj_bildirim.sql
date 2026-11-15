-- =============================================================================
-- Mavi Takım - Twitter Klonu örnek mesaj ve bildirim verisi
--
-- NE İÇİN: Arayüzdeki "Mesajlar" ve "Bildirimler" bölümleri geliştirme ve demo
-- sırasında boş görünmesin diye örnek veri yükler. Uygulama bu dosyaya ihtiyaç
-- duymaz.
--
-- NASIL: Önce şu dosyalar çalıştırılmış olmalı:
--   1. schema.sql (yeni kurulum) ya da changes/001_mesajlar_bildirimler.sql (mevcut veritabanı)
--   2. seed.sql (örnek kullanıcılar, tweetler, beğeniler, takipler)
-- Ardından bu dosya Supabase SQL Editor'de elle çalıştırılır. messages veya
-- notifications tablosunda kayıt varsa hiçbir şey yapmadan hata verir (veri iki
-- kez yüklenmesin diye).
--
-- İÇERİK:
--   - Bildirimler mevcut veriden türetilir, böylece akışla tutarlıdır:
--       * like   : başkasının tweetine yapılmış her beğeni
--       * reply  : başkasının tweetine yazılmış her (silinmemiş) yanıt
--       * follow : her takip ilişkisi
--     Son 2 günden eski bildirimler okunmuş, daha yeniler okunmamış sayılır.
--     Seed dışındaki kullanıcıların hareketleri de bildirime dönüşür.
--     Yalnızca seed.sql verisiyle: yaklaşık 500 bildirim, 100 kadarı okunmamış.
--   - 10 sohbette 47 özel mesaj, son ~5 güne yayılmış. 7 mesaj okunmamış.
--       * ayse_kaya: 4 sohbet, 2'sinde okunmamış mesaj var (rozet testi)
--       * cem_ozturk -> ayse_kaya: tek yönlü, cevapsız sohbet
--       * 1 mesaj HTML/script içeriyor: arayüzde düz metin olarak görünmeli (XSS testi)
--       * yeni_uye: mesajı yok, yalnızca 5 takip bildirimi var (boş mesaj ekranını test etmek için)
--
-- Tarihler çalıştırıldığı ana göre hesaplanır.
-- =============================================================================

begin;

do $$
begin
  if not exists (select 1 from public.users where username = 'ayse_kaya')
     or not exists (select 1 from public.users where username = 'onur_koc') then
    raise exception 'Örnek kullanıcılar bulunamadı: önce seed.sql çalıştırılmalı.';
  end if;

  if exists (select 1 from public.messages) or exists (select 1 from public.notifications) then
    raise exception 'messages veya notifications boş değil: bu dosya yalnızca bir kez yüklenir.';
  end if;

  -- Bildirimler: beğeniler ------------------------------------------------------
  insert into public.notifications (recipient_id, actor_id, type, tweet_id, created_at)
  select t.user_id, l.user_id, 'like', t.id, l.created_at
  from public.likes l
  join public.tweets t on t.id = l.tweet_id
  where t.user_id <> l.user_id
    and t.deleted_at is null;

  -- Bildirimler: yanıtlar -------------------------------------------------------
  insert into public.notifications (recipient_id, actor_id, type, tweet_id, created_at)
  select p.user_id, r.user_id, 'reply', r.id, r.created_at
  from public.tweets r
  join public.tweets p on p.id = r.parent_id
  where p.user_id <> r.user_id
    and r.deleted_at is null;

  -- Bildirimler: takipler -------------------------------------------------------
  insert into public.notifications (recipient_id, actor_id, type, tweet_id, created_at)
  select f.following_id, f.follower_id, 'follow', null, f.created_at
  from public.follows f;

  -- Son 2 günden eski bildirimler okunmuş olsun
  update public.notifications
  set read_at = least(created_at + interval '1 hour', now())
  where created_at < now() - interval '2 days';

  -- Mesajlar -------------------------------------------------------------------
  -- gonderen/alici: seed.sql'deki kullanıcı adları
  -- dk_once: mesajın kaç dakika önce gönderildiği
  -- okundu: alıcı mesajı okudu mu
  insert into public.messages (sender_id, recipient_id, content, created_at, read_at)
  select s.id, r.id, m.icerik,
         now() - m.dk_once * interval '1 minute',
         case when m.okundu then least(now() - m.dk_once * interval '1 minute' + interval '4 minutes', now()) end
  from (values
    -- 1) ayse_kaya <-> mehmet_demir: deploy
    ('mehmet_demir', 'ayse_kaya',    7000, true,  'Selam Ayşe, yeni endpoint''ler test ortamında, bakabilir misin?'),
    ('ayse_kaya',    'mehmet_demir', 6990, true,  'Selam! Bakarım, Swagger linki aynı mı?'),
    ('mehmet_demir', 'ayse_kaya',    6985, true,  'Aynı, sadece /api/feed altına iki yeni route ekledim.'),
    ('ayse_kaya',    'mehmet_demir', 6900, true,  'Baktım, cursor parametresi boş gelince 500 dönüyor.'),
    ('mehmet_demir', 'ayse_kaya',    6890, true,  'Hmm, null kontrolünü unutmuşum. Hemen düzeltiyorum.'),
    ('mehmet_demir', 'ayse_kaya',    6800, true,  'Düzeldi, testi de ekledim 👍'),
    ('ayse_kaya',    'mehmet_demir', 6795, true,  'Süper, teşekkürler. Cuma deploy etmeyelim ama 😄'),
    ('mehmet_demir', 'ayse_kaya',    6790, true,  'Söz, pazartesi sabah.'),

    -- 2) zeynep_ak <-> onur_koc: hafta sonu planı
    ('onur_koc',  'zeynep_ak', 4300, true,  'Hafta sonu Belgrad Ormanı''na gidiyor muyuz?'),
    ('zeynep_ak', 'onur_koc',  4250, true,  'Gidelim! Sen bisikletle mi geleceksin?'),
    ('onur_koc',  'zeynep_ak', 4245, true,  'Evet, sabah 8 gibi girişte olurum.'),
    ('zeynep_ak', 'onur_koc',  4200, true,  'Tamam, ben de fotoğraf makinesini getiriyorum 📷'),
    ('onur_koc',  'zeynep_ak', 300,  false, 'Hava yağmurlu görünüyor, pazara mı alsak?'),

    -- 3) elif_sahin <-> ayse_kaya: tasarım
    ('elif_sahin', 'ayse_kaya',  2900, true,  'Profil sayfasının yeni tasarımını Figma''ya yükledim.'),
    ('ayse_kaya',  'elif_sahin', 2880, true,  'Çok iyi olmuş, özellikle boş durum ekranı.'),
    ('elif_sahin', 'ayse_kaya',  2875, true,  'Teşekkürler! Gri tonlarını da azalttım, artık 4 tane var.'),
    ('ayse_kaya',  'elif_sahin', 2870, true,  '14''ten 4''e, tebrikler 😄'),
    ('elif_sahin', 'ayse_kaya',  1500, true,  'Mobil görünüm için de bir taslak hazırlarım.'),

    -- 4) emre_kurt <-> burak_celik: oyun jam
    ('emre_kurt',   'burak_celik', 5200, true,  'Gelecek ayki oyun jam''e katılıyor musun?'),
    ('burak_celik', 'emre_kurt',   5100, true,  'Katılmak isterim ama oyun geliştirmeden pek anlamam.'),
    ('emre_kurt',   'burak_celik', 5095, true,  'Sorun değil, skor tablosu ve istatistik kısmını sen yaparsın.'),
    ('burak_celik', 'emre_kurt',   5090, true,  'Bu olur, veri kısmı benden.'),
    ('emre_kurt',   'burak_celik', 900,  true,  'Kaydımızı yaptım, takım adı: Piksel Veri.'),
    ('burak_celik', 'emre_kurt',   880,  false, 'Harika isim 👾'),

    -- 5) gizem_polat <-> selin_arslan: tarif ve kitap
    ('selin_arslan', 'gizem_polat',  3600, true,  'Mantı tarifini alabilir miyim? Fotoğrafı çok güzeldi.'),
    ('gizem_polat',  'selin_arslan', 3550, true,  'Tabii! Hamuru ince açmak tek sır, gerisi sabır.'),
    ('selin_arslan', 'gizem_polat',  3545, true,  'Karşılığında sana bir kitap önerisi: Kürk Mantolu Madonna.'),
    ('gizem_polat',  'selin_arslan', 3500, true,  'Okumadım, hemen listeme ekliyorum 📚'),
    ('selin_arslan', 'gizem_polat',  120,  false, 'Mantıyı denedim, un her yerde 😅'),

    -- 6) can_yildiz <-> deniz_aydin: derbi
    ('can_yildiz',  'deniz_aydin', 2200, true,  'Derbiyi nerede izliyoruz?'),
    ('deniz_aydin', 'can_yildiz',  2190, true,  'Kadıköy''de bir yer biliyorum, ekranı büyük.'),
    ('can_yildiz',  'deniz_aydin', 2185, true,  'Tamam, 20.30''da orada olalım.'),
    ('deniz_aydin', 'can_yildiz',  1300, true,  'Maç bitti, sesim kısıldı.'),
    ('can_yildiz',  'deniz_aydin', 1290, true,  'Hakem kararları yine konuşulur ama olsun.'),

    -- 7) kerem_dogan <-> mehmet_demir: docker
    ('kerem_dogan',  'mehmet_demir', 4000, true,  'Backend imajı neden 1 GB? 😱'),
    ('mehmet_demir', 'kerem_dogan',  3990, true,  'SDK imajıyla çalıştırıyorum galiba.'),
    ('kerem_dogan',  'mehmet_demir', 3985, true,  'Multi-stage build yap, runtime imajı yeter. Örnek Dockerfile atıyorum.'),
    ('mehmet_demir', 'kerem_dogan',  3800, true,  '180 MB''a indi, teşekkürler!'),
    ('kerem_dogan',  'mehmet_demir', 60,   false, 'Log seviyelerini de bir ara konuşalım.'),

    -- 8) ece_yilmaz <-> ayse_kaya: podcast daveti (son mesaj okunmamış)
    ('ece_yilmaz', 'ayse_kaya',  2000, true,  'Merhaba Ayşe, podcast''e konuk olmak ister misin?'),
    ('ayse_kaya',  'ece_yilmaz', 1950, true,  'Merhaba! Konu ne olacak?'),
    ('ece_yilmaz', 'ayse_kaya',  1945, true,  'Yazılımcılarda erteleme alışkanlığı. Senin deneyimlerin çok işe yarar.'),
    ('ayse_kaya',  'ece_yilmaz', 1940, true,  'Çok sevinirim. Hangi gün uygun?'),
    ('ece_yilmaz', 'ayse_kaya',  45,   false, 'Perşembe 14.00 nasıl? Kayıt yaklaşık 40 dakika sürer.'),

    -- 9) cem_ozturk -> ayse_kaya: tek yönlü, cevapsız
    ('cem_ozturk', 'ayse_kaya', 600, true,  'Merhaba, C# öğrenmeye yeni başladım. Kaynak önerebilir misin?'),
    ('cem_ozturk', 'ayse_kaya', 30,  false, '<b>kalın</b> <script>alert("mesaj")</script> bu mesaj düz metin görünmeli'),

    -- 10) zeynep_ak -> elif_sahin: tek mesaj, okunmamış
    ('zeynep_ak', 'elif_sahin', 10, false, 'Kapadokya fotoğraflarını gönderdim, kapak görseli için bakar mısın?')
  ) as m(gonderen, alici, dk_once, okundu, icerik)
  join public.users s on s.username = m.gonderen
  join public.users r on r.username = m.alici;

  raise notice 'Yüklendi: % bildirim (% okunmamış), % mesaj (% okunmamış)',
    (select count(*) from public.notifications),
    (select count(*) from public.notifications where read_at is null),
    (select count(*) from public.messages),
    (select count(*) from public.messages where read_at is null);
end;
$$;

commit;

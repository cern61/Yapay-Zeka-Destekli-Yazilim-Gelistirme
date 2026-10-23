-- =============================================================================
-- Mavi Takım - Twitter Klonu örnek veri (seed)
--
-- NE İÇİN: Geliştirme, elle test ve demo sırasında uygulamanın boş görünmemesi
-- için gerçekçi örnek veri yükler. Uygulama bu dosyaya ihtiyaç duymaz.
--
-- NASIL: Önce schema.sql çalıştırılmış olmalı. Ardından bu dosya Supabase SQL
-- Editor'de elle çalıştırılır. Yalnızca BOŞ veritabanına yüklenir; tablolarda
-- kullanıcı varsa hiçbir şey yapmadan hata verir (veri iki kez yüklenmesin diye).
--
-- İÇERİK:
--   - 15 kullanıcı, hepsinin şifresi: Test1234
--       * yeni_uye: tweeti yok, kimseyi takip etmiyor (boş durum ekranlarını test etmek için)
--       * cem_ozturk: bio boş (boş alan gösterimini test etmek için)
--   - 90 tweet ve 55 yanıt (yanıta yanıt ve 3 seviyeli zincirler dahil), son ~19 güne yayılmış
--   - 3 silinmiş tweet (2'sinin yanıtları var: "yanıtlanan tweet silindi" durumu)
--   - 1 tweet HTML/script içeriyor: arayüzde düz metin olarak görünmeli (XSS testi)
--   - Takip ilişkileri ve beğeniler (en yeni tweetlerden biri herkes tarafından beğenilmiş)
--
-- Kullanıcı ID'leri sabittir (00000000-0000-0000-0000-0000000000NN), böylece
-- testlerde ve sorgularda kolayca kullanılabilir. Tarihler çalıştırıldığı ana
-- göre hesaplanır, böylece akış her zaman "yakın zamanda" görünür.
-- =============================================================================

begin;

do $$
declare
  -- Tüm kullanıcılar için aynı BCrypt hash'i (Test1234). BCrypt.Net ile doğrulanabilir.
  pw_hash text := extensions.crypt('Test1234', extensions.gen_salt('bf', 10));

  tweet_texts text[] := array[
    'Bugün ilk tweetimi atıyorum, herkese merhaba! 👋',
    'Sabah kahvesi olmadan tek satır kod yazamıyorum ☕',
    'Belgrad Ormanı''nda yürüyüş yaptık, hava mükemmeldi 🌲',
    'Derbi akşamı kimse beni aramasın ⚽',
    'Kullanıcıya üç tıklamadan fazla iş yaptıran her ekran yeniden tasarlanmalı.',
    'Pandas ile 2 milyon satırlık veriyi okuyunca bilgisayar kalkışa geçti 🚀',
    'Bu ay 4 kitap bitirdim. Sırada Sabahattin Ali var.',
    'Unity''de fizik motoruyla 3 saattir uğraşıyorum, top hâlâ duvarın içinden geçiyor.',
    'Kadıköy vapurunda martılara simit atmayın, onlar için zararlı 🐦',
    'Hafta sonu planı: hiçbir plan yapmamak.',
    'Mercimek çorbasına bir tutam kimyon ve limon, gerisi detay 🍋',
    'Production''a cuma akşamı deploy yapan ekip arkadaşına sevgilerle 🙃',
    'Yeni podcast bölümü yayında: Erteleme alışkanlığı nereden geliyor?',
    'Sahil yolunda 40 km pedal çevirdim, bacaklar yarın konuşacak 🚴',
    'Git commit mesajına sadece "düzeltme" yazmak yasaklanmalı.',
    'C# 12''deki primary constructor özelliğini sevdim, kod çok sadeleşti.',
    'Kapadokya''da balonların kalkışını izlemek her şeye değerdi 🎈',
    'Bu sezon şampiyonluk yarışı son haftaya kalacak gibi.',
    'Karanlık mod bir tercih değil, bir yaşam biçimidir 🌙',
    'Makine öğrenmesi dersinde ilk modelim yüzde 51 doğrulukla çalıştı. Yazı tura atmaktan iyi!',
    'Kitapçıda bir saat geçirip hiçbir şey almadan çıkmak da bir sanat.',
    'Oyun jam''inde 48 saatte bir platform oyunu çıkardık 🎮',
    'Ankara''da sonbahar başka güzel oluyor 🍂',
    'Bazen en iyi hata ayıklama yöntemi biraz yürüyüşe çıkmak.',
    'Ev yapımı mantı denedim. Sonuç: un her yerde, mantı tabakta 🥟',
    'Docker imajını 1.2 GB''tan 180 MB''a indirdim, multi-stage build harika bir şey.',
    'Uykusuzluk üzerine okuduğum makale beni uykusuz bıraktı.',
    'Şehirde bisiklet yolu olmayınca bisikletli olmak cesaret işi.',
    'Kod incelemesinde "küçük bir öneri" diye başlayan yorumlardan korkun.',
    'LINQ sorgusu yazarken kendimi şair gibi hissediyorum.',
    'Gün batımında Galata Köprüsü ✨',
    'Milli maç günü sokaklar bomboş.',
    'Tasarım sisteminde 14 farklı gri ton bulduk. Biri bize açıklama yapsın.',
    'Veri temizliği, veri biliminin yüzde 80''i. Kalan yüzde 20 de veri temizliğinden şikâyet etmek.',
    'Kütüphanede sessizlik kuralına uymayan tek şey midemdi.',
    'Oyuncular oyunu hiç düşünmediğin şekilde oynar. Her zaman.',
    'Sabah 6''da kalkıp spor yapanlara saygı duyuyorum, ama uzaktan.',
    '<script>alert("merhaba")</script> bu yazı ekranda düz metin olarak görünmeli 😉',
    'Fırından yeni çıkmış ekmeğin kokusu terapi yerine geçer 🍞',
    'Sunucu loglarına bakmadan "bende çalışıyor" demek yok.',
    'İnsanlar neden toplantı sonunda "eklemek isteyen var mı?" sorusuna gerçekten cevap verir?',
    'Bugün 25 km, rüzgâr arkamdaydı, kendimi profesyonel sandım.',
    'Tek satırlık değişiklik, üç saatlik test. Yazılım böyle bir şey.',
    'async/await''i ilk anladığım gün hayatım değişti.',
    'Kaçkar Dağları listemde bir numara. Bu yaz kesin gidiyorum 🏔️',
    'Hakem kararları yine konuşulacak gibi.',
    'Boş durum ekranları da tasarımın parçası. "Henüz tweet yok" yazısı bile güzel olabilir.',
    'Grafikteki eksenleri etiketlemeyen herkese sesleniyorum: lütfen.',
    'Bir kitabı, filmini izlemeden önce okumak gerekir. Tartışmaya kapalıdır.',
    'Piksel art karakterime yürüme animasyonu ekledim, gururluyum 👾',
    'Rüzgâr çıktı, Moda''da dalgalar sahile vuruyor 🌊',
    'Bugünün sözü: Önce çalıştır, sonra güzelleştir.',
    'Menemen soğanlı mı olur soğansız mı? Cevaplar yanıtlarda.',
    'Kubernetes YAML dosyası yazarken boşluk sayısını yanlış yapan ben.',
    'Dinlemek, konuşmaktan daha zor bir beceri.',
    'Lastik patladı, yol kenarında tamir ettim. Artık yedek iç lastik taşıyorum.',
    'Regex yazdım, çalıştı, ama nasıl çalıştığını bilmiyorum.',
    'Unit test yazmak başta yavaşlatıyor gibi görünür, sonra hayat kurtarır.',
    'Sahafta 1965 baskısı bir Orhan Veli buldum 📖',
    'Bu tweet silinecek, test amaçlı.',
    'Kamp ateşi, gitar ve yıldızlar. Başka bir şey istemem 🔥',
    'Transfer dönemi başladı, söylentiler de.',
    'Erişilebilirlik bir özellik değil, bir gereklilik.',
    'SQL''de JOIN''leri anladığımda bir üst seviyeye geçtim.',
    'Okuma listem okuyabileceğimden hızlı büyüyor.',
    'Ses efektleri oyunun yarısıdır. Sessiz bir oyunu oynamayı deneyin.',
    'Bugün Boğaz''da yunus gördüm! 🐬',
    'Yemek tarifinde "göz kararı" yazanlar bana bir ölçü versin lütfen.',
    'Monitörü dikey çevirdim, kod okumak daha kolay oldu.',
    'İyi bir uyku, en iyi verimlilik uygulamasıdır 😴',
    'Yokuş yukarı pedal çevirirken hayatı sorguluyorum.',
    'İnternet kesilince kod yazmaya çalışmak: hafızam kadar programcıyım.',
    'Yeni projemizde ASP.NET Core ve PostgreSQL kullanıyoruz, şimdiye kadar çok memnunuz.',
    'Fotoğraf çekerken altın saat gerçekten altın değerinde 📷',
    'Maç bitti, sesim kısıldı.',
    'Form hata mesajları kullanıcıyı suçlamamalı. "Geçersiz giriş" yerine ne yapması gerektiğini söyleyin.',
    'Korelasyon nedensellik değildir. Bunu her gün kendime hatırlatıyorum.',
    'Yağmurlu bir gün, sıcak çay ve iyi bir roman ☔',
    'Oyunumun demo sürümünü arkadaşlarıma oynattım, herkes ilk bölümde kayboldu.',
    'Ege''de bir köyde zeytin hasadına katıldık 🫒',
    'Takvimde boş bir gün görünce mutlu oluyorum.',
    'Fırında karnıyarık, yanında cacık. Pazar keyfi.',
    'Log seviyelerini doğru ayarlamayan her proje bir gün sizi gece 3''te uyandırır.',
    'Bir alışkanlığı bırakmak için onun yerine yenisini koymak gerekiyormuş.',
    'Bisikletle işe gitmek hem spor hem trafiksiz yolculuk.',
    'Kodu yazan için değil, okuyan için yazın.',
    'Yeni klavye aldım, yazdığım her şey daha önemliymiş gibi geliyor ⌨️',
    'Bu uygulamada 280 karakter sınırı var, bu yüzden düşüncelerimi kısa tutmaya çalışıyorum. Uzun yazmak kolay, kısa yazmak zor. Bir fikri az kelimeyle anlatabiliyorsan onu gerçekten anlamışsın demektir. Bu tweet de sınırı denemek için uzun yazıldı, okuduğun için teşekkürler!',
    'Az önce hesabımı açtım, burası güzel görünüyor.',
    'İyi geceler herkese 🌙'
  ];

  -- 1-40: tweetlere yanıt, 41-50: yanıtlara yanıt, 51-55: yanıtın yanıtına yanıt
  reply_texts text[] := array[
    'Kesinlikle katılıyorum!',
    'Bunu ben de yaşadım 😄',
    'Harika görünüyor, nerede bu?',
    'Bence tam tersi, ama saygı duyuyorum.',
    'Hahaha çok doğru',
    'Bir dahakine beni de çağır!',
    'Kaynak paylaşabilir misin?',
    'Tebrikler 👏',
    'Bunu okuyunca güldüm',
    'Aynen öyle, her seferinde.',
    'Ben de denemek istiyorum, tarifi atar mısın?',
    'Bu konuda bir yazı yazsana.',
    'Hiç bu açıdan düşünmemiştim.',
    'Geçen hafta ben de aynısını yaptım.',
    'Bence biraz abartılıyor.',
    '🔥🔥🔥',
    'Kolay gelsin!',
    'Benim de favorim.',
    'İkinci kısmına katılmıyorum.',
    'Fotoğrafı da paylaş lütfen 📷',
    'Bu tweeti kaydettim.',
    'Ne zaman yapıyoruz?',
    'Beni de etiketle bir dahakine.',
    'Gerçekten mi? İnanamadım.',
    'Çok iyi bir hatırlatma, teşekkürler.',
    'Bunu yazdığın için teşekkürler.',
    'Kim yapmadı ki bunu 😅',
    'Başarılar diliyorum!',
    'Aynı fikirdeyim, özellikle ilk kısım.',
    'Bunu takım arkadaşlarıma da göstereceğim.',
    'Peki sence çözüm ne?',
    'Bence denemeye değer.',
    'Bu ara herkes bundan bahsediyor.',
    'Haklısın ama her zaman mümkün olmuyor.',
    'Bir de benim yöntemimi dene.',
    'Bu çok tanıdık geldi.',
    'Eline sağlık!',
    'Mükemmel 👌',
    'Şaka mı yapıyorsun?',
    'Ben de oradaydım!',
    'Aynen, ben de bunu diyecektim.',
    'Yanıtına katılıyorum.',
    'Sanırım yanlış anladın, demek istediği farklı.',
    'İkiniz de haklısınız aslında.',
    'Bu tartışmayı sevdim 😄',
    'Resmi dokümantasyon bu konuda en iyi kaynak.',
    'Tamam, ikna oldum.',
    'Bir dahaki sefere hep beraber.',
    'Aynen öyle!',
    'Güzel nokta.',
    'Teşekkürler, işime yaradı.',
    'Hahaha bu da iyiydi',
    'Katılıyorum.',
    'Bunu düşünmemiştim.',
    'Sonunda biri söyledi!'
  ];

  tweet_ids uuid[] := '{}';
  reply_ids uuid[] := '{}';
  total int;
  new_id uuid;
  parent uuid;
  parent_author uuid;
  parent_created timestamptz;
  author int;
begin
  if exists (select 1 from public.users) then
    raise exception 'Veritabanı boş değil: seed.sql yalnızca boş şemaya yüklenir.';
  end if;

  -- Kullanıcılar ----------------------------------------------------------------
  insert into public.users (id, username, display_name, email, password_hash, bio, location, website, avatar_url, created_at, updated_at)
  select format('00000000-0000-0000-0000-%s', lpad(n::text, 12, '0'))::uuid,
         u.username, u.display_name, u.username || '@example.com', pw_hash,
         u.bio, u.location, u.website, u.avatar_url,
         now() - interval '30 days' + n * interval '1 hour',
         now() - interval '30 days' + n * interval '1 hour'
  from (values
    (1,  'ayse_kaya',    'Ayşe Kaya',    'Yazılım mühendisi. Kahve ve kod.', 'İstanbul', 'https://example.com/ayse', 'https://i.pravatar.cc/150?img=47'),
    (2,  'mehmet_demir', 'Mehmet Demir', 'Backend geliştirici · .NET',       'Ankara',   null,                        'https://i.pravatar.cc/150?img=12'),
    (3,  'zeynep_ak',    'Zeynep Ak',    'Fotoğraf, doğa, yürüyüş',          'İzmir',    null,                        'https://i.pravatar.cc/150?img=45'),
    (4,  'can_yildiz',   'Can Yıldız',   'Futbol ve teknoloji',              null,       null,                        null),
    (5,  'elif_sahin',   'Elif Şahin',   'UX tasarımcısı',                   'Eskişehir','https://example.com/elif',  'https://i.pravatar.cc/150?img=32'),
    (6,  'burak_celik',  'Burak Çelik',  'Veri bilimi öğrencisi',            'Bursa',    null,                        null),
    (7,  'selin_arslan', 'Selin Arslan', 'Kitap kurdu 📚',                   null,       null,                        'https://i.pravatar.cc/150?img=44'),
    (8,  'emre_kurt',    'Emre Kurt',    'Bağımsız oyun geliştirici',        'Antalya',  'https://example.com/emre',  null),
    (9,  'deniz_aydin',  'Deniz Aydın',  'Denizci değilim, isim öyle.',      'İstanbul', null,                        null),
    (10, 'cem_ozturk',   'Cem Öztürk',   null,                               null,       null,                        null),
    (11, 'gizem_polat',  'Gizem Polat',  'Mutfakta deneyler',                'Gaziantep',null,                        'https://i.pravatar.cc/150?img=26'),
    (12, 'kerem_dogan',  'Kerem Doğan',  'DevOps · Linux',                   'Kocaeli',  null,                        null),
    (13, 'ece_yilmaz',   'Ece Yılmaz',   'Psikoloji · podcast',              'İzmir',    'https://example.com/ece',   null),
    (14, 'onur_koc',     'Onur Koç',     'Bisiklet ve şehir',                'Ankara',   null,                        'https://i.pravatar.cc/150?img=68'),
    (15, 'yeni_uye',     'Yeni Üye',     null,                               null,       null,                        null)
  ) as u(n, username, display_name, bio, location, website, avatar_url);

  -- Tweetler: ilk metin en eski, son metin en yeni. Her 5 saatte bir tweet.
  -- Yazarlar 1-14 arasında sırayla döner (15 = yeni_uye hiç tweet atmaz).
  total := array_length(tweet_texts, 1);
  for i in 1..total loop
    insert into public.tweets (user_id, content, created_at)
    values (format('00000000-0000-0000-0000-%s', lpad((((i - 1) % 14) + 1)::text, 12, '0'))::uuid,
            tweet_texts[i],
            now() - (total - i + 1) * interval '5 hours' + ((i * 17) % 60) * interval '1 minute')
    returning id into new_id;
    tweet_ids := tweet_ids || new_id;
  end loop;

  -- Yanıtlar ------------------------------------------------------------------
  for j in 1..array_length(reply_texts, 1) loop
    if j <= 40 then
      parent := tweet_ids[total - ((j * 3) % 30)];   -- en yeni 30 tweetten birine yanıt
    else
      parent := reply_ids[j - 40];                   -- 41-50: yanıta yanıt, 51-55: onların yanıtına yanıt
      if j > 50 then parent := reply_ids[j - 10]; end if;
    end if;

    select t.user_id, t.created_at into parent_author, parent_created
    from public.tweets t where t.id = parent;

    -- Kişi kendi tweetine yanıt vermesin
    author := ((j * 5) % 14) + 1;
    if format('00000000-0000-0000-0000-%s', lpad(author::text, 12, '0'))::uuid = parent_author then
      author := (author % 14) + 1;
    end if;

    insert into public.tweets (user_id, parent_id, content, created_at)
    values (format('00000000-0000-0000-0000-%s', lpad(author::text, 12, '0'))::uuid,
            parent,
            reply_texts[j],
            least(parent_created + ((j % 50) + 3) * interval '1 minute', now()))
    returning id into new_id;
    reply_ids := reply_ids || new_id;
  end loop;

  -- Silinmiş tweetler (soft delete). İlk ikisinin yanıtları var.
  update public.tweets
  set deleted_at = least(created_at + interval '2 hours', now()), content = null
  where id in (tweet_ids[total - 3], tweet_ids[total - 12], tweet_ids[60]);

  -- Takipler ------------------------------------------------------------------
  -- Herkes ayse_kaya'yı ve kendisinden 1, 2, 3, 5, 8 sonraki kullanıcıyı takip eder.
  -- yeni_uye (15) kimseyi takip etmez.
  insert into public.follows (follower_id, following_id, created_at)
  select a.id, b.id, now() - ((a.n * b.n) % 25 + 1) * interval '1 day'
  from (select id, right(id::text, 12)::int as n from public.users) a
  join (select id, right(id::text, 12)::int as n from public.users) b
    on a.id <> b.id
   and a.n <> 15
   and (b.n = 1 or ((b.n - a.n + 15) % 15) in (1, 2, 3, 5, 8));

  -- Beğeniler (yalnızca silinmemiş tweetlere) ----------------------------------
  -- Yaklaşık her 5 kullanıcı-tweet çiftinden biri beğeni.
  insert into public.likes (user_id, tweet_id, created_at)
  select u.id, t.id, least(t.created_at + (u.n * 11) * interval '1 minute', now())
  from (select id, right(id::text, 12)::int as n from public.users where username <> 'yeni_uye') u
  cross join (select id, created_at, row_number() over (order by created_at) as rn
              from public.tweets where deleted_at is null) t
  where (t.rn * 7 + u.n * 3) % 5 = 0;

  -- En yeni tweetlerden biri herkes tarafından beğenilmiş olsun ("popüler tweet")
  insert into public.likes (user_id, tweet_id, created_at)
  select u.id, tweet_ids[total - 1], now()
  from public.users u
  where u.username <> 'yeni_uye'
  on conflict do nothing;

  raise notice 'Seed yüklendi: % kullanıcı, % tweet (% yanıt, % silinmiş), % takip, % beğeni',
    (select count(*) from public.users),
    (select count(*) from public.tweets),
    (select count(*) from public.tweets where parent_id is not null),
    (select count(*) from public.tweets where deleted_at is not null),
    (select count(*) from public.follows),
    (select count(*) from public.likes);
end;
$$;

commit;

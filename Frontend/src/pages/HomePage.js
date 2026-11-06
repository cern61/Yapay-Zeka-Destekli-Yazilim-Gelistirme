import { renderTweetCard } from '../components/TweetCard.js';
export function renderHomePage() {
    return `
    <div class="app-layout">

      <!-- SOL MENÜ -->
      <aside class="sidebar">
        <div class="logo">X</div>

        <nav class="navigation">
          <a href="#" class="nav-item active">Ana Sayfa</a>
          <a href="#" class="nav-item">Keşfet</a>
          <a href="#" class="nav-item">Bildirimler</a>
          <a href="#" class="nav-item">Mesajlar</a>
          <a href="#" class="nav-item">Yer İşaretleri</a>
          <a href="#" class="nav-item">Profil</a>
        </nav>

        <button class="post-button">Gönderi Oluştur</button>
      </aside>

      <!-- ORTA ALAN -->
      <main class="feed">

        <header class="feed-header">
          <h1>Ana Sayfa</h1>
        </header>

        <!-- GÖNDERİ OLUŞTURMA -->
        <section class="tweet-composer">
          <textarea
            placeholder="Neler oluyor?"
            aria-label="Gönderi metni"
          ></textarea>

          <div class="composer-actions">
            <button type="button">Medya</button>
            <button type="button">Anket</button>
            <button type="button" class="share-button">
              Paylaş
            </button>
          </div>
        </section>

        <!-- GÖNDERİ AKIŞI -->
        <section class="tweet-feed">

        
            ${renderTweetCard({
        displayName: 'Yunus',
        username: '@yunus',
        avatar: 'Y',
        content: 'Yapay zeka destekli sosyal medya projemizin frontend geliştirmelerine başladık.'
    })}
          

        </section>

      </main>

      <!-- SAĞ PANEL -->
      <aside class="right-panel">

        <input
          type="search"
          placeholder="Ara"
          aria-label="Ara"
        />

        <section class="trends">
          <h2>Gündem</h2>

          <div class="trend-item">
            <span>Teknoloji</span>
            <strong>Yapay Zeka</strong>
          </div>

          <div class="trend-item">
            <span>Yazılım</span>
            <strong>JavaScript</strong>
          </div>
        </section>

        <section class="suggestions">
          <h2>Önerilen Kullanıcılar</h2>
        </section>

      </aside>

    </div>
  `;
}
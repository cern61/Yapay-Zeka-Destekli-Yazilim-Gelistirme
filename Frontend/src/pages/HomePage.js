import { renderSidebar } from '../components/Sidebar.js';
import { renderTweetComposer } from '../components/TweetComposer.js';

export function renderHomePage() {
  return `
    <div class="app-layout">

      <!-- SOL MENÜ -->
      ${renderSidebar()}

      <!-- ORTA ALAN -->
      <main class="feed">

        <header class="feed-header">
          <h1>Ana Sayfa</h1>
        </header>

        <!-- GÖNDERİ OLUŞTURMA -->
        ${renderTweetComposer()}

        <!-- GÖNDERİ AKIŞI -->
        <section class="tweet-feed">

            <p class="feed-status">
              Gönderiler yükleniyor...
            </p>
          

        </section>

      <button
  type="button"
  id="load-more-tweets"
  class="load-more-button"
  hidden
>
  Daha Fazla Gönderi Yükle
</button>  
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
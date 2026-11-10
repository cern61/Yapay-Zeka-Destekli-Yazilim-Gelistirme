export function renderSidebar() {
  return `
    <aside class="sidebar">

      <div class="logo">X</div>

      <nav class="navigation">

        <a href="#home" class="nav-item active">
          Ana Sayfa
        </a>

        <a href="#notifications" class="nav-item">
          Bildirimler
        </a>

        <a href="#messages" class="nav-item">
          Mesajlar
        </a>

        <a href="#profile" class="nav-item">
          Profil
        </a>

      </nav>

      <button class="post-button" type="button">
        Gönderi Oluştur
      </button>

    </aside>
  `;
}
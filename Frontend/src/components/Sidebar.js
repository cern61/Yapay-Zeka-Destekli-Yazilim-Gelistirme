
export function renderSidebar() {
  return `
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

      <button class="post-button" type="button">
        Gönderi Oluştur
      </button>

    </aside>
  `;
}

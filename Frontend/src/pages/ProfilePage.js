
function escapeHTML(value) {
  return String(value ?? '').replace(/[&<>"']/g, (character) => {
    const entities = {
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      '"': '&quot;',
      "'": '&#39;'
    };

    return entities[character];
  });
}

function getSafeURL(value) {
  if (!value) return '';

  try {
    const url = new URL(value);

    return ['http:', 'https:'].includes(url.protocol)
      ? url.href
      : '';
  } catch {
    return '';
  }
}

export function renderProfilePage(profile = {}) {
  const displayName = profile.displayName || 'Kullanıcı';
  const username = (profile.username || 'kullanici').replace(/^@/, '');
  const avatar = displayName.trim().charAt(0).toUpperCase() || 'K';

  const tweetCount = Math.max(0, Number(profile.tweetCount) || 0);
  const followerCount = Math.max(0, Number(profile.followerCount) || 0);
  const followingCount = Math.max(0, Number(profile.followingCount) || 0);

  const websiteURL = getSafeURL(profile.website);
  const avatarURL = getSafeURL(profile.avatarUrl);

  const avatarHTML = avatarURL
    ? `<img src="${escapeHTML(avatarURL)}" alt="${escapeHTML(displayName)}">`
    : escapeHTML(avatar);

  const websiteHTML = websiteURL
    ? `
      <p class="profile-website">
        <a href="${escapeHTML(websiteURL)}"
           target="_blank"
           rel="noopener noreferrer">
          ${escapeHTML(profile.website)}
        </a>
      </p>
    `
    : '';

  return `
    <section class="profile-page">

      <header class="profile-header">
        <a href="#" class="profile-back">←</a>
        <h1>Profil</h1>
      </header>

      <div class="profile-cover"></div>

      <section class="profile-details">

        <div class="profile-avatar">
          ${avatarHTML}
        </div>

        <div class="profile-actions">
          <button type="button" id="edit-profile-button">
            Profili Düzenle
          </button>
        </div>

        <h2>${escapeHTML(displayName)}</h2>

        <p class="profile-username">
          @${escapeHTML(username)}
        </p>

        <p class="profile-bio">
          ${escapeHTML(profile.bio || 'Henüz biyografi eklenmemiş.')}
        </p>

        ${profile.location ? `
          <p class="profile-location">
            ${escapeHTML(profile.location)}
          </p>
        ` : ''}

        ${websiteHTML}

        <div class="profile-stats">
          <span>
            <strong>${tweetCount}</strong> Gönderi
          </span>

          <span>
            <strong>${followingCount}</strong> Takip edilen
          </span>

          <span>
            <strong>${followerCount}</strong> Takipçi
          </span>
        </div>

      </section>

      <form id="profile-edit-form" class="profile-edit-form" hidden>

        <h2>Profili Düzenle</h2>
        <input
  type="hidden"
  name="avatarUrl"
  value="${escapeHTML(profile.avatarUrl || '')}"
>
        

        <div class="form-group">
          <label for="edit-display-name">Görünen Ad</label>
          <input
            type="text"
            id="edit-display-name"
            name="displayName"
            value="${escapeHTML(displayName)}"
            maxlength="50"
            required
          >
        </div>

        <div class="form-group">
          <label for="edit-bio">Biyografi</label>
          <textarea
            id="edit-bio"
            name="bio"
            maxlength="160"
            rows="3"
          >${escapeHTML(profile.bio || '')}</textarea>
        </div>

        <div class="form-group">
          <label for="edit-location">Konum</label>
          <input
            type="text"
            id="edit-location"
            name="location"
            value="${escapeHTML(profile.location || '')}"
            maxlength="30"
          >
        </div>

        <div class="form-group">
          <label for="edit-website">Web Sitesi</label>
          <input
            type="url"
            id="edit-website"
            name="website"
            value="${escapeHTML(profile.website || '')}"
            maxlength="100"
            placeholder="https://example.com"
          >
        </div>

                <div class="form-group">
          <label for="edit-avatar-file">Profil Fotoğrafı</label>

          <input
            type="file"
            id="edit-avatar-file"
            accept="image/jpeg,image/png,image/webp"
          >

          <small>
            JPEG, PNG veya WebP. Maksimum 4 MB.
          </small>

          <button
            type="button"
            id="upload-avatar-button"
          >
            Fotoğrafı Yükle
          </button>

          <p
            id="avatar-upload-message"
            aria-live="polite"
          ></p>
        </div>

        <div class="profile-edit-actions">
          <button type="submit" id="save-profile-button">
            Kaydet
          </button>

          <button type="button" id="cancel-profile-edit">
            İptal
          </button>
        </div>

        <p id="profile-edit-message" aria-live="polite"></p>

      </form>

      <nav class="profile-tabs">
        <span class="active">Gönderiler</span>
      </nav>

      <section class="profile-posts">
        <p class="feed-status">
          Bu kullanıcının gönderileri yükleniyor...
        </p>
      </section>

    </section>
  `;
}

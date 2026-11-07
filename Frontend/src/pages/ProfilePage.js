
import { renderTweetCard } from '../components/TweetCard.js';

export function renderProfilePage() {
  return `
    <section class="profile-page">

      <header class="profile-header">
        <a href="/" class="profile-back">←</a>
        <h1>Profil</h1>
      </header>

      <div class="profile-cover"></div>

      <section class="profile-details">

        <div class="profile-avatar">Y</div>

        <h2>Yunus</h2>
        <p class="profile-username">@yunus</p>

        <p class="profile-bio">
          Yazılım ve yapay zeka teknolojileriyle ilgileniyorum.
        </p>

        <div class="profile-stats">
          <span>
            <strong>0</strong> Takip edilen
          </span>

          <span>
            <strong>0</strong> Takipçi
          </span>
        </div>

      </section>

      <nav class="profile-tabs">
        <span class="active">Gönderiler</span>
      </nav>

      <section class="profile-posts">

        ${renderTweetCard({
          displayName: 'Yunus',
          username: '@yunus',
          avatar: 'Y',
          content: 'Yapay zeka destekli sosyal medya projemizin frontend geliştirmelerine başladık.'
        })}

      </section>

    </section>
  `;
}

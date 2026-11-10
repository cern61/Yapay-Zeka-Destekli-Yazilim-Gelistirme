
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

export function renderTweetCard(tweet = {}) {
  const {
    id = '',
    displayName = 'Yunus',
    username = '@yunus',
    avatar = 'Y',
    content = '',
    likeCount = 0,
    replyCount = 0,
    isLiked = false,
    isOwner = false
  } = tweet;

  const safeLikeCount = Math.max(0, Number(likeCount) || 0);
  const safeReplyCount = Math.max(0, Number(replyCount) || 0);
  const safeUsername = String(username || '@kullanici').trim().replace(/^@/, '') || 'kullanici';

  return `
    <article
      class="tweet-card"
      data-tweet-id="${escapeHTML(id)}"
    >

      <div class="tweet-user">
  <div class="avatar">${escapeHTML(avatar)}</div>

  <div>
    <strong>${escapeHTML(displayName)}</strong>
    <span>
      <a
        class="tweet-profile-link"
        href="#profile/${encodeURIComponent(safeUsername)}"
      >@${escapeHTML(safeUsername)}</a>
    </span>
  </div>
</div>

      <p>${escapeHTML(content)}</p>

      

            <div class="tweet-actions">

        <button type="button" data-action="reply">
          Yanıtla
          <span class="reply-count">${safeReplyCount}</span>
        </button>

        <button
          type="button"
          data-action="like"
          class="${isLiked ? 'is-liked' : ''}"
          aria-pressed="${Boolean(isLiked)}"
          aria-label="${isLiked ? 'Beğeniyi kaldır' : 'Beğen'}"
        >
          <span class="like-icon" aria-hidden="true">${isLiked ? '♥' : '♡'}</span>
          <span class="like-count">${safeLikeCount}</span>
        </button>

        ${isOwner ? '<button type="button" data-action="delete" aria-label="Gönderiyi sil">Sil</button>' : ''}

        <button type="button" data-action="share">
          Paylaş
        </button>

      </div>

      <form class="reply-composer" hidden>
        <textarea
          maxlength="280"
          placeholder="Yanıtını yaz..."
          aria-label="Yanıtın"
        ></textarea>

        <div class="reply-controls">
          <span class="reply-counter">0/280</span>

          <button type="submit" disabled>
            Yanıtla
          </button>
        </div>
      </form>

      <div class="tweet-replies"></div>

    </article>
  `;
}

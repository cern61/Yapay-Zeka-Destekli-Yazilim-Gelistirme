
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
    displayName = 'Yunus',
    username = '@yunus',
    avatar = 'Y',
    content = ''
  } = tweet;

  return `
    <article class="tweet-card">

      <div class="tweet-user">
        <div class="avatar">${escapeHTML(avatar)}</div>

        <div>
          <strong>${escapeHTML(displayName)}</strong>
          <span>${escapeHTML(username)}</span>
        </div>
      </div>

      <p>${escapeHTML(content)}</p>

      <div class="tweet-actions">

        <button type="button" data-action="reply">
          Yanıtla
        </button>

        <button
          type="button"
          data-action="like"
          aria-pressed="false"
        >
          Beğen <span class="like-count">0</span>
        </button>

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

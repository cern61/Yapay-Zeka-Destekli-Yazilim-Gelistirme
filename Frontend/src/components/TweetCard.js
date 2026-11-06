export function renderTweetCard(tweet = {}) {
  const {
    displayName = 'Yunus',
    username = '@yunus',
    avatar = 'Y',
    content = 'Yapay zeka destekli sosyal medya projemizin frontend geliştirmelerine başladık.'
  } = tweet;

  return `
    <article class="tweet-card">

      <div class="tweet-user">
        <div class="avatar">${avatar}</div>

        <div>
          <strong>${displayName}</strong>
          <span>${username}</span>
        </div>
      </div>

      <p>${content}</p>

      <div class="tweet-actions">
        <button type="button">Yanıtla</button>
        <button type="button">Beğen</button>
        <button type="button">Paylaş</button>
      </div>

    </article>
  `;
}
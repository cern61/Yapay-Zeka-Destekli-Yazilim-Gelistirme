
export function renderTweetComposer() {
  return `
    <section class="tweet-composer">

      <textarea
        id="tweet-content"
        maxlength="280"
        placeholder="Neler oluyor?"
        aria-label="Gönderi metni"
      ></textarea>

      <div class="composer-actions">

        <span id="tweet-counter" class="tweet-counter">
          0/280
        </span>

        <button
          type="button"
          id="share-tweet"
          class="share-button"
          disabled
        >
          Paylaş
        </button>

      </div>

    </section>
  `;
}

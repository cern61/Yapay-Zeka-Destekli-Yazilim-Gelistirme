
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

        <button
          type="button"
          class="share-button"
        >
          Paylaş
        </button>

      </div>

    </section>
  `;
}

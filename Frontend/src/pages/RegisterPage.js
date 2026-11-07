
export function renderRegisterPage() {
  return `
    <section class="auth-page">

      <div class="auth-card">

        <div class="auth-logo">X</div>

        <h1>Hesap Oluştur</h1>
        <p>Aramıza katıl ve paylaşmaya başla.</p>

        <form id="register-form">

          <div class="form-group">
            <label for="register-display-name">
              Görünen Ad
            </label>

            <input
              type="text"
              id="register-display-name"
              name="displayName"
              placeholder="Adın"
              autocomplete="name"
              required
            />
          </div>

          <div class="form-group">
            <label for="register-username">
              Kullanıcı Adı
            </label>

            <input
              type="text"
              id="register-username"
              name="username"
              placeholder="kullanici_adi"
              minlength="3"
              maxlength="15"
              pattern="[a-z0-9_]{3,15}"
              title="3-15 karakter, küçük harf, rakam ve alt çizgi kullanın."
              autocomplete="username"
              required
            />
          </div>

          <div class="form-group">
            <label for="register-email">
              E-posta
            </label>

            <input
              type="email"
              id="register-email"
              name="email"
              placeholder="ornek@mail.com"
              autocomplete="email"
              required
            />
          </div>

          <div class="form-group">
            <label for="register-password">
              Şifre
            </label>

            <input
              type="password"
              id="register-password"
              name="password"
              placeholder="Şifreni oluştur"
              autocomplete="new-password"
              required
            />
          </div>

          <button type="submit" class="auth-submit">
            Kayıt Ol
          </button>

        </form>

        <p id="register-message" class="auth-message"
   role="status" aria-live="polite"></p>

        <p class="auth-switch">
          Zaten hesabın var mı?
          <a href="#login">Giriş Yap</a>
        </p>

      </div>

    </section>
  `;
}

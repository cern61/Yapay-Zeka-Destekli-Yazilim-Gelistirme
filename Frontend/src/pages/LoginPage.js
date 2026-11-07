
export function renderLoginPage() {
    return `
    <section class="auth-page">

      <div class="auth-card">

        <div class="auth-logo">X</div>

        <h1>Giriş Yap</h1>
        <p>Hesabına giriş yap ve sohbete katıl.</p>

        <form id="login-form">

          <div class="form-group">
            <label for="login-identifier">
              Kullanıcı adı veya e-posta
            </label>

            <input
              type="text"
              id="login-identifier"
              name="identifier"
              placeholder="Kullanıcı adın veya e-posta"
              autocomplete="username"
              required
            />
          </div>

          <div class="form-group">
            <label for="login-password">Şifre</label>

            <input
              type="password"
              id="login-password"
              name="password"
              placeholder="Şifren"
              autocomplete="current-password"
              required
            />
          </div>

          <button type="submit" class="auth-submit">
            Giriş Yap
          </button>

        </form>

        <p id="login-message" class="auth-message"
   role="status" aria-live="polite"></p>
   
        <p class="auth-switch">
          Hesabın yok mu?
          <a href="#register">Kayıt Ol</a>
        </p>

      </div>

    </section>
  `;
}


import './style.css';
import { renderProfilePage } from './pages/ProfilePage.js';
import { renderHomePage } from './pages/HomePage.js';
import { renderLoginPage } from './pages/LoginPage.js';
import { renderRegisterPage } from './pages/RegisterPage.js';

import {
  loginUser,
  registerUser
} from './services/authService.js';

const app = document.querySelector('#app');

// Hata mesajlarını ekranda gösterir.
function showMessage(elementId, message) {
  const element = document.getElementById(elementId);

  if (element) {
    element.textContent = message;
  }
}

// Login form işlemi
function bindLoginForm() {
  const form = document.querySelector('#login-form');

  if (!form) return;

  form.addEventListener('submit', async (event) => {
    event.preventDefault();

    const button = form.querySelector('button[type="submit"]');
    const formData = new FormData(form);

    button.disabled = true;
    button.textContent = 'Giriş yapılıyor...';

    showMessage('login-message', '');

    try {
      await loginUser({
        login: formData.get('identifier'),
        password: formData.get('password')
      });

      window.location.hash = '';

    } catch (error) {
      showMessage('login-message', error.message);

    } finally {
      button.disabled = false;
      button.textContent = 'Giriş Yap';
    }
  });
}

// Register form işlemi
function bindRegisterForm() {
  const form = document.querySelector('#register-form');

  if (!form) return;

  form.addEventListener('submit', async (event) => {
    event.preventDefault();

    const button = form.querySelector('button[type="submit"]');
    const formData = new FormData(form);

    button.disabled = true;
    button.textContent = 'Kayıt yapılıyor...';

    showMessage('register-message', '');

    try {
      await registerUser({
        username: formData.get('username'),
        displayName: formData.get('displayName'),
        email: formData.get('email'),
        password: formData.get('password')
      });

      window.location.hash = '';

    } catch (error) {
      showMessage('register-message', error.message);

    } finally {
      button.disabled = false;
      button.textContent = 'Kayıt Ol';
    }
  });
}

// Sayfa yönlendirme
function renderApp() {
  if (!app) return;

  switch (window.location.hash) {
    case '#login':
      app.innerHTML = renderLoginPage();
      bindLoginForm();
      break;

    case '#register':
      app.innerHTML = renderRegisterPage();
      bindRegisterForm();
      break;

    default:
      app.innerHTML = renderHomePage();
      break;
  }
}

window.addEventListener('hashchange', renderApp);

renderApp();

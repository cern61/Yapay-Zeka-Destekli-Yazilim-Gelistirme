import './style.css';

import { renderHomePage } from './pages/HomePage.js';
import { renderLoginPage } from './pages/LoginPage.js';
import { renderRegisterPage } from './pages/RegisterPage.js';

const app = document.querySelector('#app');

// Sayfa yönlendirme işlemi
function renderApp() {
  if (!app) return;

  const route = window.location.hash;

  switch (route) {
    case '#login':
      app.innerHTML = renderLoginPage();
      break;

    case '#register':
      app.innerHTML = renderRegisterPage();
      break;

    default:
      app.innerHTML = renderHomePage();
      break;
  }
}

// URL değiştiğinde sayfayı güncelle
window.addEventListener('hashchange', renderApp);

// Uygulamayı başlat
renderApp();
import './style.css'
import { renderHomePage } from './pages/HomePage.js'

// Uygulama alanını bul
const app = document.querySelector('#app')

// Ana sayfayı ekrana yükle
if (app) {
  app.innerHTML = renderHomePage()
}
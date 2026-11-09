
import './style.css';
import { renderProfilePage } from './pages/ProfilePage.js';
import { renderHomePage } from './pages/HomePage.js';
import { renderLoginPage } from './pages/LoginPage.js';
import { renderRegisterPage } from './pages/RegisterPage.js';
import { renderTweetCard } from './components/TweetCard.js';
import { getPublicFeed } from './services/feedService.js';
import {
  getUserProfile,
  getUserTweets,
  updateMyProfile,
  uploadAvatar
} from './services/profileService.js';

import { createTweet } from './services/tweetService.js';

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

// Tweet oluşturma alanı

function bindTweetComposer() {
  const textarea = document.querySelector('#tweet-content');
  const counter = document.querySelector('#tweet-counter');
  const button = document.querySelector('#share-tweet');
  const feed = document.querySelector('.tweet-feed');

  if (!textarea || !counter || !button || !feed) return;

  let isSubmitting = false;

  // Gönderi oluşturma kısayolu
  const postShortcut = document.querySelector('.post-button');

  if (postShortcut) {
    postShortcut.addEventListener('click', () => {
      textarea.scrollIntoView({
        behavior: 'smooth',
        block: 'center'
      });
      textarea.focus();
    });
  }

  function updateComposer() {
    const length = textarea.value.length;

    counter.textContent = `${length}/280`;
    button.disabled =
      isSubmitting || textarea.value.trim().length === 0 || length > 280;
  }

  textarea.addEventListener('input', updateComposer);

  // Gerçek API üzerinden gönderi oluştur
  button.addEventListener('click', async () => {
    const content = textarea.value.trim();

    if (!content || isSubmitting) return;

    isSubmitting = true;
    updateComposer();

    try {
      await createTweet(content);

      textarea.value = '';
      updateComposer();

      // Akışı veritabanından yeniden yükle
      await loadPublicFeed();

    } catch (error) {
      alert(error.message);
    } finally {
      isSubmitting = false;
      updateComposer();
    }
  });

  updateComposer();
}

function bindTweetInteractions() {
  if (!app) return;

  // Beğenme ve yanıt alanını açma
  app.addEventListener('click', (event) => {
    const button = event.target.closest('button[data-action]');

    if (!button) return;

    const card = button.closest('.tweet-card');

    if (!card) return;

    if (button.dataset.action === 'like') {
      const liked = button.getAttribute('aria-pressed') === 'true';
      const counter = button.querySelector('.like-count');

      const currentCount = Number(counter.textContent);

      button.setAttribute('aria-pressed', String(!liked));
      counter.textContent = String(
        Math.max(0, currentCount + (liked ? -1 : 1))
      );

      button.classList.toggle('is-liked', !liked);
    }

    if (button.dataset.action === 'reply') {
      const form = card.querySelector('.reply-composer');

      form.hidden = !form.hidden;

      if (!form.hidden) {
        form.querySelector('textarea').focus();
      }
    }
  });

  // Yanıt karakter sayacı
  app.addEventListener('input', (event) => {
    const textarea = event.target.closest('.reply-composer textarea');

    if (!textarea) return;

    const form = textarea.closest('.reply-composer');
    const counter = form.querySelector('.reply-counter');
    const button = form.querySelector('button[type="submit"]');

    counter.textContent = `${textarea.value.length}/280`;
    button.disabled = textarea.value.trim().length === 0;
  });

  // Yanıt gönderme (şimdilik yerel)
  app.addEventListener('submit', (event) => {
    const form = event.target.closest('.reply-composer');

    if (!form) return;

    event.preventDefault();

    const textarea = form.querySelector('textarea');
    const content = textarea.value.trim();

    if (!content) return;

    const card = form.closest('.tweet-card');
    const replies = card.querySelector('.tweet-replies');

    const reply = document.createElement('p');

    reply.className = 'tweet-reply';
    reply.textContent = content;

    replies.appendChild(reply);

    textarea.value = '';
    form.querySelector('.reply-counter').textContent = '0/280';
    form.querySelector('button[type="submit"]').disabled = true;

    form.hidden = true;
  });
}


// Backend üzerinden gerçek gönderileri yükle
async function loadPublicFeed() {
  const feed = document.querySelector('.tweet-feed');

  if (!feed) return;

  feed.textContent = 'Gönderiler yükleniyor...';

  try {
    const page = await getPublicFeed();

    if (!feed.isConnected) return;

    const tweets = page.items.filter(tweet =>
      !tweet.isDeleted &&
      typeof tweet.content === 'string' &&
      tweet.content.trim().length > 0
    );

    if (tweets.length === 0) {
      feed.textContent = 'Henüz gönderi bulunmuyor.';
      return;
    }

    feed.innerHTML = tweets.map(tweet => {
      const author = tweet.author ?? {};
      const displayName = author.displayName || 'Kullanıcı';
      const rawUsername = author.username || 'kullanici';

      return renderTweetCard({
        id: tweet.id,
        displayName,
        username: rawUsername.startsWith('@')
          ? rawUsername
          : `@${rawUsername}`,
        avatar: displayName.trim().charAt(0).toUpperCase(),
        content: tweet.content,
        likeCount: tweet.likeCount,
        replyCount: tweet.replyCount,
        isLiked: tweet.isLiked,
        parentId: tweet.parentId,
        parentContent: tweet.parentContent,
        parentDeleted: tweet.parentDeleted
      });
    }).join('');

  } catch (error) {
    if (!feed.isConnected) return;

    feed.textContent =
      `Gönderiler yüklenemedi: ${error.message}`;
  }
}




async function loadProfile(username) {
  const currentHash = window.location.hash;

  app.innerHTML = `
    <section class="profile-page">
      <p class="feed-status">Profil yükleniyor...</p>
    </section>
  `;

  try {
    // 1. Profil bilgilerini getir
    const profile = await getUserProfile(username);

    if (window.location.hash !== currentHash) return;

    // Profil arayüzünü oluştur
    app.innerHTML = renderProfilePage(profile);

    // 2. Profil düzenleme elemanları
    const editButton = app.querySelector('#edit-profile-button');
    const editForm = app.querySelector('#profile-edit-form');
    const cancelButton = app.querySelector('#cancel-profile-edit');
    const message = app.querySelector('#profile-edit-message');

    // Profil fotoğrafı yükleme elemanları
    const avatarInput = app.querySelector('#edit-avatar-file');
    const uploadButton = app.querySelector('#upload-avatar-button');
    const avatarMessage = app.querySelector('#avatar-upload-message');
    // Seçilen fotoğrafın anlık önizlemesi
    let avatarPreviewUrl = null;

    avatarInput?.addEventListener('change', () => {
      const file = avatarInput.files?.[0];

      if (!file) return;

      if (!['image/jpeg', 'image/png', 'image/webp'].includes(file.type)) {
        avatarMessage.textContent = 'Geçersiz fotoğraf formatı.';
        avatarInput.value = '';
        return;
      }

      if (file.size > 4 * 1024 * 1024) {
        avatarMessage.textContent = 'Fotoğraf 4 MB sınırını aşamaz.';
        avatarInput.value = '';
        return;
      }

      if (avatarPreviewUrl) {
        URL.revokeObjectURL(avatarPreviewUrl);
      }

      avatarPreviewUrl = URL.createObjectURL(file);

      const avatarContainer = app.querySelector('.profile-avatar');

      if (!avatarContainer) return;

      let image = avatarContainer.querySelector('img');

      if (!image) {
        image = document.createElement('img');
        avatarContainer.replaceChildren(image);
      }

      image.src = avatarPreviewUrl;
      image.alt = 'Profil fotoğrafı önizlemesi';

      avatarMessage.textContent = 'Fotoğraf önizlemesi hazır.';
    });

    // Fotoğraf yükleme işlemi
    uploadButton?.addEventListener('click', async () => {
      const file = avatarInput?.files?.[0];

      if (!file) {
        avatarMessage.textContent = 'Lütfen bir fotoğraf seçin.';
        return;
      }

      uploadButton.disabled = true;
      avatarMessage.textContent = 'Fotoğraf yükleniyor...';
      avatarMessage.style.color = '#1d9bf0';

      try {
        await uploadAvatar(file);

        if (window.location.hash !== currentHash) return;

        // Geçici fotoğraf adresini temizle
        if (avatarPreviewUrl) {
          URL.revokeObjectURL(avatarPreviewUrl);
          avatarPreviewUrl = null;
        }

        // Güncellenen profili yeniden yükle
        await loadProfile(username);

      } catch (error) {
        avatarMessage.textContent = error.message;
        avatarMessage.style.color = '#f4212e';

      } finally {
        if (uploadButton.isConnected) {
          uploadButton.disabled = false;
        }
      }
    });

    // Düzenleme formunu aç
    editButton?.addEventListener('click', () => {
      editForm.hidden = false;

      editForm.scrollIntoView({
        behavior: 'smooth',
        block: 'center'
      });

      editForm.querySelector('[name="displayName"]')?.focus();
    });

    // Düzenlemeyi iptal et
    cancelButton?.addEventListener('click', () => {
      editForm.reset();
      editForm.hidden = true;
      message.textContent = '';
    });

    // Profili API üzerinden güncelle
    editForm?.addEventListener('submit', async (event) => {
      event.preventDefault();

      const formData = new FormData(editForm);
      const saveButton = app.querySelector('#save-profile-button');

      saveButton.disabled = true;
      message.textContent = 'Profil güncelleniyor...';

      try {
        await updateMyProfile({
          displayName: formData.get('displayName'),
          bio: formData.get('bio'),
          location: formData.get('location'),
          website: formData.get('website'),
          avatarUrl: formData.get('avatarUrl')
        });

        if (window.location.hash !== currentHash) return;

        // Güncellenen profili yeniden getir
        await loadProfile(username);

      } catch (error) {
        if (window.location.hash !== currentHash) return;

        message.textContent = error.message;
        message.style.color = 'red';

      } finally {
        if (saveButton.isConnected) {
          saveButton.disabled = false;
        }
      }
    });

    // 3. Kullanıcının gönderilerini getir
    const postsContainer = app.querySelector('.profile-posts');

    if (!postsContainer) return;

    postsContainer.textContent = 'Gönderiler yükleniyor...';

    const page = await getUserTweets(username);

    if (window.location.hash !== currentHash) return;

    // Silinmiş ve boş gönderileri filtrele
    const tweets = page.items.filter(tweet =>
      !tweet.isDeleted &&
      typeof tweet.content === 'string' &&
      tweet.content.trim().length > 0
    );

    if (tweets.length === 0) {
      postsContainer.textContent = 'Henüz gönderi bulunmuyor.';
      return;
    }

    // 4. TweetCard bileşenlerini oluştur
    postsContainer.innerHTML = tweets.map(tweet => {
      const author = tweet.author ?? {};

      const displayName =
        author.displayName || profile.displayName || 'Kullanıcı';

      const rawUsername =
        author.username || profile.username || 'kullanici';

      return renderTweetCard({
        id: tweet.id,
        displayName,
        username: rawUsername.startsWith('@')
          ? rawUsername
          : `@${rawUsername}`,
        avatar: displayName.trim().charAt(0).toUpperCase(),
        content: tweet.content,
        likeCount: tweet.likeCount,
        replyCount: tweet.replyCount,
        isLiked: tweet.isLiked,
        parentId: tweet.parentId,
        parentContent: tweet.parentContent,
        parentDeleted: tweet.parentDeleted
      });
    }).join('');

  } catch (error) {
    if (window.location.hash !== currentHash) return;

    const postsContainer = app.querySelector('.profile-posts');

    if (postsContainer) {
      postsContainer.textContent =
        `Gönderiler yüklenemedi: ${error.message}`;
    } else {
      app.textContent = `Profil yüklenemedi: ${error.message}`;
    }
  }
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
    case '#profile': {
      const currentUser = JSON.parse(
        sessionStorage.getItem('authUser') || 'null'
      );

      if (!currentUser || !currentUser.username) {
        window.location.hash = '#login';
        break;
      }

      loadProfile(currentUser.username);
      break;
    }

    default:
      app.innerHTML = renderHomePage();
      bindTweetComposer();
      loadPublicFeed();
      break;
  }
}

bindTweetInteractions();

window.addEventListener('hashchange', renderApp);

renderApp();

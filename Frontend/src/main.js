
import './style.css';

import { renderProfilePage } from './pages/ProfilePage.js';
import { renderHomePage } from './pages/HomePage.js';
import { renderLoginPage } from './pages/LoginPage.js';
import { renderRegisterPage } from './pages/RegisterPage.js';
import { renderTweetCard } from './components/TweetCard.js';
import { getHomeFeed } from './services/feedService.js';
import {
  getUserProfile,
  getUserTweets,
  updateMyProfile,
  uploadAvatar,
  followUser,
  unfollowUser,
  getUserFollowers,
  getUserFollowing
} from './services/profileService.js';
import {
  createTweet,
  likeTweet,
  unlikeTweet,
  getTweetReplies,
  deleteTweet
} from './services/tweetService.js';

import {
  loginUser,
  registerUser
} from './services/authService.js';

const app = document.querySelector('#app');
let homeFeedCursor = null;
let isLoadingHomeFeed = false;
let homeFeedHasMore = true;

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

      window.location.hash = '#home';

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

      window.location.hash = '#login';

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

  // Beğeni ve yanıt alanını açma
  app.addEventListener('click', async (event) => {
    const target = event.target;

    if (!(target instanceof Element)) return;

    const button = target.closest('button[data-action]');

    if (!button) return;

    const card = button.closest('.tweet-card');

    if (!card) return;

    // GERÇEK API ÜZERİNDEN GÖNDERİ SİLME
    if (button.dataset.action === 'delete') {
      const tweetId = card.dataset.tweetId;

      if (!tweetId) {
        alert('Gönderi ID bulunamadı.');
        return;
      }

      const confirmed = confirm(
        'Bu gönderiyi silmek istediğine emin misin?'
      );

      if (!confirmed) return;

      button.disabled = true;

      try {
        await deleteTweet(tweetId);
        card.remove();

      } catch (error) {
        alert(error.message);

      } finally {
        if (button.isConnected) {
          button.disabled = false;
        }
      }

      return;
    }

    // GERÇEK API İLE BEĞENME / BEĞENİYİ KALDIRMA
    if (button.dataset.action === 'like') {
      const tweetId = card.dataset.tweetId;
      const liked = button.getAttribute('aria-pressed') === 'true';
      const counter = button.querySelector('.like-count');

      if (!tweetId || !counter) {
        alert('Gönderi bilgileri bulunamadı.');
        return;
      }

      const currentCount = Number(counter.textContent) || 0;

      // Tekrarlanan istekleri engelle
      if (button.disabled) return;

      button.disabled = true;

      try {
        if (liked) {
          await unlikeTweet(tweetId);
        } else {
          await likeTweet(tweetId);
        }

        const newLiked = !liked;

        // API başarılı olduktan sonra arayüzü güncelle
        button.setAttribute('aria-pressed', String(newLiked));

        counter.textContent = String(
          Math.max(0, currentCount + (newLiked ? 1 : -1))
        );

        button.classList.toggle('is-liked', newLiked);

        const likeIcon = button.querySelector('.like-icon');

        if (likeIcon) {
          likeIcon.textContent = newLiked ? '♥' : '♡';
        }

        button.setAttribute(
          'aria-label',
          newLiked ? 'Beğeniyi kaldır' : 'Beğen'
        );

      } catch (error) {
        alert(error.message);

      } finally {
        if (button.isConnected) {
          button.disabled = false;
        }
      }
    }


    // YANIT FORMUNU AÇ / KAPAT VE API'DEN YANITLARI GETİR
    if (button.dataset.action === 'reply') {
      const form = card.querySelector('.reply-composer');
      const repliesContainer = card.querySelector('.tweet-replies');

      if (!form || !repliesContainer) return;

      form.hidden = !form.hidden;

      if (form.hidden) return;

      form.querySelector('textarea')?.focus();

      // Yanıtlar daha önce yüklendiyse tekrar istek gönderme
      if (repliesContainer.dataset.loaded === 'true') return;

      const tweetId = card.dataset.tweetId;

      if (!tweetId) {
        repliesContainer.textContent = 'Gönderi ID bulunamadı.';
        return;
      }

      repliesContainer.textContent = 'Yanıtlar yükleniyor...';

      try {
        const page = await getTweetReplies(tweetId);

        if (!card.isConnected) return;

        const replies = page.items.filter(reply =>
          !reply.isDeleted &&
          !reply.parentDeleted &&
          typeof reply.content === 'string' &&
          reply.content.trim().length > 0
        );

        repliesContainer.replaceChildren();

        if (replies.length === 0) {
          repliesContainer.textContent = 'Henüz yanıt bulunmuyor.';
        } else {
          replies.forEach(reply => {
            const item = document.createElement('div');
            item.className = 'tweet-reply';

            const author = reply.author ?? {};

            const authorName = document.createElement('strong');
            authorName.textContent = author.displayName || 'Kullanıcı';

            const username = document.createElement('span');
            username.textContent =
              ` @${(author.username || 'kullanici').replace(/^@/, '')}`;

            const content = document.createElement('p');
            content.textContent = reply.content;

            item.append(authorName, username, content);
            repliesContainer.appendChild(item);
          });
        }

        repliesContainer.dataset.loaded = 'true';

      } catch (error) {
        repliesContainer.textContent =
          `Yanıtlar yüklenemedi: ${error.message}`;
      }
    }
  });

  // YANIT KARAKTER SAYACI
  app.addEventListener('input', (event) => {
    const target = event.target;

    if (!(target instanceof Element)) return;

    const textarea = target.closest('.reply-composer textarea');

    if (!textarea) return;

    const form = textarea.closest('.reply-composer');
    const counter = form.querySelector('.reply-counter');
    const button = form.querySelector('button[type="submit"]');

    counter.textContent = `${textarea.value.length}/280`;

    button.disabled =
      textarea.value.trim().length === 0 ||
      textarea.value.length > 280;
  });

  // GERÇEK API ÜZERİNDEN YANIT GÖNDERME
  app.addEventListener('submit', async (event) => {
    const target = event.target;

    if (!(target instanceof HTMLFormElement)) return;

    const form = target.closest('.reply-composer');

    if (!form) return;

    event.preventDefault();

    const textarea = form.querySelector('textarea');
    const content = textarea.value.trim();
    const card = form.closest('.tweet-card');
    const tweetId = card?.dataset.tweetId;
    const repliesContainer = card?.querySelector('.tweet-replies');
    const submitButton = form.querySelector('button[type="submit"]');
    const counter = form.querySelector('.reply-counter');

    if (!content || !tweetId || !repliesContainer) return;
    if (submitButton.disabled) return;

    submitButton.disabled = true;
    submitButton.textContent = 'Gönderiliyor...';

    try {
      // Yanıtı ana gönderinin ID'si ile API'ye gönder
      await createTweet(content, tweetId);

      // Formu temizle
      textarea.value = '';
      counter.textContent = '0/280';
      form.hidden = true;

      // Yanıt sayısını artır
      const replyCount = card.querySelector('.reply-count');

      if (replyCount) {
        replyCount.textContent = String(
          (Number(replyCount.textContent) || 0) + 1
        );
      }

      // Eski yanıt listesini geçersiz kıl
      delete repliesContainer.dataset.loaded;

      // Yanıtları yeniden API'den getir
      try {
        const page = await getTweetReplies(tweetId);

        if (!card.isConnected) return;

        repliesContainer.replaceChildren();

        if (page.items.length === 0) {
          repliesContainer.textContent = 'Henüz yanıt bulunmuyor.';
        } else {
          page.items
            .filter(reply =>
              !reply.isDeleted &&
              !reply.parentDeleted &&
              typeof reply.content === 'string' &&
              reply.content.trim().length > 0
            )
            .forEach(reply => {
              const item = document.createElement('div');
              item.className = 'tweet-reply';

              const author = reply.author ?? {};

              const authorName = document.createElement('strong');
              authorName.textContent = author.displayName || 'Kullanıcı';

              const username = document.createElement('span');
              username.textContent =
                ` @${(author.username || 'kullanici').replace(/^@/, '')}`;

              const replyContent = document.createElement('p');
              replyContent.textContent = reply.content;

              item.append(authorName, username, replyContent);
              repliesContainer.appendChild(item);
            });
        }

        repliesContainer.dataset.loaded = 'true';

      } catch (error) {
        repliesContainer.textContent =
          `Yanıt kaydedildi ancak liste yenilenemedi: ${error.message}`;
      }

    } catch (error) {
      alert(error.message);

    } finally {
      if (submitButton.isConnected) {
        submitButton.textContent = 'Yanıtla';
        submitButton.disabled =
          textarea.value.trim().length === 0;
      }
    }
  });

}


// Backend üzerinden gerçek gönderileri yükle

async function loadPublicFeed(loadMore = false) {
  const feed = document.querySelector('.tweet-feed');
  const moreButton = document.querySelector('#load-more-tweets');

  if (!feed || isLoadingHomeFeed) return;

  if (loadMore && (!homeFeedHasMore || !homeFeedCursor)) return;

  if (!loadMore) {
    homeFeedCursor = null;
    homeFeedHasMore = true;
    feed.textContent = 'Gönderiler yükleniyor...';

    if (moreButton) moreButton.hidden = true;
  }

  isLoadingHomeFeed = true;

  if (moreButton) {
    moreButton.disabled = true;
    moreButton.textContent = 'Yükleniyor...';
  }

  try {
    const page = await getHomeFeed(
      loadMore ? homeFeedCursor : null
    );

    if (!feed.isConnected) return;

    const currentUser = JSON.parse(
      sessionStorage.getItem('authUser') || 'null'
    );

    const existingIds = new Set(
      [...feed.querySelectorAll('.tweet-card')]
        .map(card => card.dataset.tweetId)
    );

    const tweets = page.items.filter(tweet =>
      tweet.parentId === null &&
      !tweet.isDeleted &&
      typeof tweet.content === 'string' &&
      tweet.content.trim().length > 0 &&
      !existingIds.has(String(tweet.id))
    );

    const tweetHTML = tweets.map(tweet => {
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
        parentDeleted: tweet.parentDeleted,
        isOwner: Boolean(
          currentUser?.id &&
          author.id &&
          String(currentUser.id).toLowerCase() ===
          String(author.id).toLowerCase()
        )
      });
    }).join('');

    if (loadMore) {
      if (tweetHTML) {
        feed.insertAdjacentHTML('beforeend', tweetHTML);
      }
    } else {
      feed.innerHTML = tweetHTML || 'Henüz gönderi bulunmuyor.';
    }

    homeFeedCursor = page.nextCursor ?? null;
    homeFeedHasMore = Boolean(homeFeedCursor);

    if (moreButton) {
      moreButton.hidden = !homeFeedHasMore;
    }

  } catch (error) {
    if (!feed.isConnected) return;

    const message = document.createElement('p');
    message.className = 'feed-status';
    message.textContent = `Gönderiler yüklenemedi: ${error.message}`;

    if (loadMore) {
      feed.appendChild(message);
    } else {
      feed.replaceChildren(message);
    }

  } finally {
    isLoadingHomeFeed = false;

    if (moreButton?.isConnected) {
      moreButton.disabled = false;
      moreButton.textContent = 'Daha Fazla Gönderi Yükle';
    }
  }
}


async function loadProfile(username) {
  const currentHash = window.location.hash;

  const currentUser = JSON.parse(
    sessionStorage.getItem('authUser') || 'null'
  );

  const normalizeUsername = (value) =>
    String(value || '').trim().replace(/^@/, '').toLowerCase();

  const isOwnProfile =
    normalizeUsername(currentUser?.username) === normalizeUsername(username);

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
    app.innerHTML = renderProfilePage({ ...profile, isOwnProfile });


    const followButton = app.querySelector('#follow-user-button');

    followButton?.addEventListener('click', async () => {
      const isFollowing =
        followButton.dataset.following === 'true';

      followButton.disabled = true;
      followButton.textContent = 'İşlem yapılıyor...';

      try {
        if (isFollowing) {
          await unfollowUser(username);
        } else {
          await followUser(username);
        }

        const newFollowing = !isFollowing;

        followButton.dataset.following = String(newFollowing);
        followButton.textContent = newFollowing
          ? 'Takibi Bırak'
          : 'Takip Et';

        const followerCountElement =
          app.querySelector('#profile-follower-count');

        if (followerCountElement) {
          const count = Number(followerCountElement.textContent) || 0;

          followerCountElement.textContent = String(
            Math.max(0, count + (newFollowing ? 1 : -1))
          );
        }

      } catch (error) {
        alert(error.message);

        followButton.textContent = isFollowing
          ? 'Takibi Bırak'
          : 'Takip Et';

      } finally {
        if (followButton.isConnected) {
          followButton.disabled = false;
        }
      }
    });
    // Takipçi ve takip edilen listeleri
    const relationsContainer = app.querySelector('#profile-relations');
    const relationButtons = app.querySelectorAll('[data-profile-list]');

    relationButtons.forEach((button) => {
      button.addEventListener('click', async () => {
        if (!relationsContainer) return;

        const listType = button.dataset.profileList;
        const isFollowers = listType === 'followers';

        const title = isFollowers
          ? 'Takipçiler'
          : 'Takip edilenler';

        relationsContainer.hidden = false;
        relationsContainer.replaceChildren();

        const heading = document.createElement('h3');
        heading.textContent = title;

        const status = document.createElement('p');
        status.textContent = 'Liste yükleniyor...';

        relationsContainer.append(heading, status);

        relationButtons.forEach((item) => {
          item.disabled = true;
        });

        try {
          const users = isFollowers
            ? await getUserFollowers(username)
            : await getUserFollowing(username);

          if (!relationsContainer.isConnected ||
            window.location.hash !== currentHash) return;

          relationsContainer.replaceChildren();
          relationsContainer.appendChild(heading);

          if (users.length === 0) {
            const emptyMessage = document.createElement('p');
            emptyMessage.textContent = 'Henüz kullanıcı bulunmuyor.';
            relationsContainer.appendChild(emptyMessage);
            return;
          }

          const list = document.createElement('ul');
          list.className = 'profile-relation-list';

          users.forEach((user) => {
            const item = document.createElement('li');

            const link = document.createElement('a');

            const cleanUsername = String(user.username || '')
              .trim()
              .replace(/^@/, '');

            link.href = `#profile/${encodeURIComponent(cleanUsername)}`;
            link.textContent =
              `${user.displayName || 'Kullanıcı'} @${cleanUsername}`;

            item.appendChild(link);
            list.appendChild(item);
          });

          relationsContainer.appendChild(list);

        } catch (error) {
          status.textContent = `Liste yüklenemedi: ${error.message}`;

        } finally {
          relationButtons.forEach((item) => {
            item.disabled = false;
          });
        }
      });
    });

    if (!isOwnProfile) {
      app.querySelector('#edit-profile-button')?.remove();
      app.querySelector('#profile-edit-form')?.remove();
    }
    // 2. Profil düzenleme elemanları
    const editButton = app.querySelector('#edit-profile-button');
    const editForm = app.querySelector('#profile-edit-form');
    const cancelButton = app.querySelector('#cancel-profile-edit');
    const message = app.querySelector('#profile-edit-message');
    // Profil fotoğrafı elemanları
    const avatarInput = app.querySelector('#edit-avatar-file');
    const avatarMessage = app.querySelector('#avatar-upload-message');
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

      if (avatarMessage) {
        avatarMessage.textContent = '';
      }
    });
    // Profil bilgilerini ve fotoğrafını Kaydet butonuyla güncelle
    editForm?.addEventListener('submit', async (event) => {
      event.preventDefault();

      const formData = new FormData(editForm);
      const saveButton = app.querySelector('#save-profile-button');
      const avatarFile = avatarInput?.files?.[0] || null;

      saveButton.disabled = true;
      message.textContent = 'Profil güncelleniyor...';

      try {
        let avatarUrl = formData.get('avatarUrl') || null;

        // Yeni fotoğraf seçilmişse önce Storage'a yükle
        if (avatarFile) {
          if (avatarMessage) {
            avatarMessage.textContent = 'Fotoğraf yükleniyor...';
          }
          await uploadAvatar(avatarFile);

          // Backend'den kaydedilmiş yeni fotoğraf URL'sini al
          const updatedProfile = await getUserProfile(username);

          avatarUrl = updatedProfile.avatarUrl;

          if (avatarMessage) {
            avatarMessage.textContent = 'Fotoğraf başarıyla kaydedildi.';
          }
        }

        // Profil bilgilerini güncelle
        await updateMyProfile({
          displayName: formData.get('displayName'),
          bio: formData.get('bio'),
          location: formData.get('location'),
          website: formData.get('website'),
          avatarUrl: avatarUrl
        });

        if (window.location.hash !== currentHash) return;

        message.textContent = 'Profil başarıyla güncellendi.';

        // Güncellenmiş profili yeniden yükle
        await loadProfile(username);

      } catch (error) {
        if (window.location.hash !== currentHash) return;

        message.textContent = error.message;
        message.style.color = '#f4212e';

      } finally {
        if (saveButton.isConnected) {
          saveButton.disabled = false;
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
    // 3. Profil gönderilerini sayfalı yükle
    const postsContainer = app.querySelector('.profile-posts');
    const moreProfileButton = app.querySelector(
      '#load-more-profile-tweets'
    );

    if (!postsContainer) return;

    let profileCursor = null;
    let profileHasMore = true;
    let isLoadingProfileTweets = false;

    async function loadProfileTweets(loadMore = false) {
      if (isLoadingProfileTweets) return;

      if (loadMore && (!profileHasMore || !profileCursor)) return;

      isLoadingProfileTweets = true;

      if (!loadMore) {
        postsContainer.textContent = 'Gönderiler yükleniyor...';
      }

      if (moreProfileButton) {
        moreProfileButton.disabled = true;
        moreProfileButton.textContent = 'Yükleniyor...';
      }

      try {
        const page = await getUserTweets(
          username,
          loadMore ? profileCursor : null
        );

        if (
          window.location.hash !== currentHash ||
          !postsContainer.isConnected
        ) return;

        const existingIds = new Set(
          [...postsContainer.querySelectorAll('.tweet-card')]
            .map(card => card.dataset.tweetId)
        );

        const tweets = page.items.filter(tweet =>
          tweet.parentId === null &&
          !tweet.isDeleted &&
          typeof tweet.content === 'string' &&
          tweet.content.trim().length > 0 &&
          !existingIds.has(String(tweet.id))
        );

        const tweetHTML = tweets.map(tweet => {
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
            parentDeleted: tweet.parentDeleted,
            isOwner: Boolean(
              currentUser?.id &&
              author.id &&
              String(currentUser.id).toLowerCase() ===
              String(author.id).toLowerCase()
            )
          });
        }).join('');

        if (loadMore) {
          if (tweetHTML) {
            postsContainer.insertAdjacentHTML('beforeend', tweetHTML);
          }
        } else {
          postsContainer.innerHTML =
            tweetHTML || 'Henüz gönderi bulunmuyor.';
        }

        profileCursor = page.nextCursor ?? null;
        profileHasMore = Boolean(profileCursor);

        if (moreProfileButton) {
          moreProfileButton.hidden = !profileHasMore;
        }

      } catch (error) {
        const message = document.createElement('p');
        message.textContent =
          `Gönderiler yüklenemedi: ${error.message}`;

        postsContainer.appendChild(message);

      } finally {
        isLoadingProfileTweets = false;

        if (moreProfileButton?.isConnected) {
          moreProfileButton.disabled = false;
          moreProfileButton.textContent =
            'Daha Fazla Gönderi Yükle';
        }
      }
    }

    moreProfileButton?.addEventListener('click', () => {
      loadProfileTweets(true);
    });

    await loadProfileTweets();


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

function renderApp() {
  if (!app) return;

  const hash = window.location.hash;

  const profileMatch = hash.match(/^#profile(?:\/(.+))?$/);

  if (profileMatch) {
    const currentUser = JSON.parse(
      sessionStorage.getItem('authUser') || 'null'
    );

    const token = sessionStorage.getItem('authToken');

    const username = profileMatch[1]
      ? decodeURIComponent(profileMatch[1])
      : currentUser?.username;

    if (!token || !username) {
      window.location.hash = '#login';
      return;
    }

    loadProfile(username);
    return;
  }

  switch (hash) {
    case '#login':
      app.innerHTML = renderLoginPage();
      bindLoginForm();
      break;

    case '#register':
      app.innerHTML = renderRegisterPage();
      bindRegisterForm();
      break;


    case '#home':
      if (!sessionStorage.getItem('authToken')) {
        window.location.hash = '#login';
        return;
      }

      app.innerHTML = renderHomePage();
      bindTweetComposer();

      const moreButton = app.querySelector('#load-more-tweets');

      moreButton?.addEventListener('click', () => {
        loadPublicFeed(true);
      });

      loadPublicFeed();
      break;


    default:
      window.location.hash = '#login';
      break;
  }
}

bindTweetInteractions();

window.addEventListener('hashchange', renderApp);

renderApp();

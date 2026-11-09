
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export async function getUserProfile(username) {
  if (!API_BASE_URL) {
    throw new Error('Backend API adresi tanımlanmamış.');
  }

  if (!username || !username.trim()) {
    throw new Error('Kullanıcı adı belirtilmedi.');
  }

  const url = new URL(
    `/api/users/${encodeURIComponent(username.trim())}`,
    API_BASE_URL
  );

  let response;

  try {
    response = await fetch(url, {
      method: 'GET',
      headers: {
        Accept: 'application/json'
      }
    });
  } catch {
    throw new Error('Backend sunucusuna bağlanılamadı.');
  }

  const data = await response.json().catch(() => null);

  if (!response.ok) {
    throw new Error(
      data?.detail ||
      data?.title ||
      `Profil isteği başarısız: ${response.status}`
    );
  }

  if (!data?.username) {
    throw new Error('Profil verisi beklenen formatta değil.');
  }

  return data;
}

export async function getUserTweets(username, cursor = null) {
  if (!API_BASE_URL) {
    throw new Error('Backend API adresi tanımlanmamış.');
  }

  const url = new URL(
    `/api/users/${encodeURIComponent(username.trim())}/tweets`,
    API_BASE_URL
  );

  if (cursor) {
    url.searchParams.set('cursor', cursor);
  }

  let response;

  try {
    response = await fetch(url, {
      method: 'GET',
      headers: {
        Accept: 'application/json'
      }
    });
  } catch {
    throw new Error('Profil gönderilerine ulaşılamadı.');
  }

  const data = await response.json().catch(() => null);

  if (!response.ok) {
    throw new Error(
      data?.detail ||
      data?.title ||
      `API hatası: ${response.status}`
    );
  }

  if (!Array.isArray(data?.items)) {
    throw new Error('Gönderi verisi beklenen formatta değil.');
  }

  return data;
}

export async function updateMyProfile(profileData) {
    const token = sessionStorage.getItem('authToken');

    if (!token) {
        throw new Error('Profil düzenlemek için giriş yapmalısınız.');
    }

    const response = await fetch(
        `${API_BASE_URL.replace(/\/$/, '')}/api/users/me`,
        {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Authorization': `Bearer ${token}`
            },
            body: JSON.stringify({
                displayName: profileData.displayName,
                bio: profileData.bio || null,
                location: profileData.location || null,
                website: profileData.website || null,
                avatarUrl: profileData.avatarUrl || null
            })
        }
    );

    const data = await response.json().catch(() => null);

    if (!response.ok) {
        const errors = data?.errors
            ? Object.values(data.errors).flat().join(' ')
            : '';

        throw new Error(
            errors || data?.message || data?.title ||
            `Profil güncellenemedi. HTTP ${response.status}`
        );
    }

    return data;
}


export async function uploadAvatar(file) {
    const token = sessionStorage.getItem('authToken');

    if (!token) {
        throw new Error('Fotoğraf yüklemek için giriş yapmalısınız.');
    }

    if (!file) {
        throw new Error('Lütfen bir fotoğraf seçin.');
    }

    const allowedTypes = [
        'image/jpeg',
        'image/png',
        'image/webp'
    ];

    if (!allowedTypes.includes(file.type)) {
        throw new Error('Yalnızca JPEG, PNG ve WebP dosyaları kabul edilir.');
    }

    if (file.size > 4 * 1024 * 1024) {
        throw new Error('Fotoğraf boyutu 4 MB sınırını aşamaz.');
    }

    const formData = new FormData();
    formData.append('file', file);

    let response;

    try {
        response = await fetch(
            `${API_BASE_URL.replace(/\/$/, '')}/api/users/me/avatar`,
            {
                method: 'POST',
                headers: {
                    'Authorization': `Bearer ${token}`,
                    'Accept': 'application/json'
                },
                body: formData
            }
        );
    } catch {
        throw new Error('Fotoğraf yüklenirken sunucuya bağlanılamadı.');
    }

    const data = await response.json().catch(() => null);

    if (!response.ok) {
        throw new Error(
            data?.message ||
            data?.detail ||
            data?.title ||
            `Fotoğraf yüklenemedi. HTTP ${response.status}`
        );
    }

    return data;
}


const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export async function getPublicFeed(cursor = null) {
  if (!API_BASE_URL) {
    throw new Error('Backend API adresi tanımlanmamış.');
  }

  const url = new URL('/api/feed/public', API_BASE_URL);

  if (cursor) {
    url.searchParams.set('cursor', cursor);
  }

  const token = sessionStorage.getItem('authToken');

  let response;

  try {
    response = await fetch(url, {
      method: 'GET',
      headers: {
        Accept: 'application/json',
        ...(token ? { Authorization: `Bearer ${token}` } : {})
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
      `API isteği başarısız: ${response.status}`
    );
  }

  if (!Array.isArray(data?.items)) {
    throw new Error('Backend yanıt formatı beklenen yapıda değil.');
  }

  return data;
}

export async function getHomeFeed(cursor = null) {
  if (!API_BASE_URL) {
    throw new Error('Backend API adresi tanımlanmamış.');
  }

  const token = sessionStorage.getItem('authToken');

  if (!token) {
    throw new Error('Kişiselleştirilmiş akış için giriş yapmalısınız.');
  }

  const url = new URL('/api/feed/home', API_BASE_URL);

  if (cursor) {
    url.searchParams.set('cursor', cursor);
  }

  let response;

  try {
    response = await fetch(url, {
      method: 'GET',
      headers: {
        Accept: 'application/json',
        Authorization: `Bearer ${token}`
      }
    });
  } catch {
    throw new Error('Kişiselleştirilmiş akışa ulaşılamadı.');
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
    throw new Error('Akış verisi beklenen formatta değil.');
  }

  return data;
}


const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

async function sendAuthRequest(endpoint, payload) {
  if (!API_BASE_URL) {
    throw new Error('Backend API adresi tanımlanmamış.');
  }

  let response;

  try {
    response = await fetch(
      `${API_BASE_URL.replace(/\/$/, '')}/api/auth/${endpoint}`,
      {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(payload)
      }
    );
  } catch {
    throw new Error('Backend sunucusuna bağlanılamadı.');
  }

  const data = await response.json().catch(() => null);

  if (!response.ok) {
    const validationErrors = data?.errors
      ? Object.values(data.errors).flat().join(' ')
      : '';

    throw new Error(
      validationErrors ||
      data?.detail ||
      data?.title ||
      `İşlem başarısız. HTTP ${response.status}`
    );
  }

  if (!data?.accessToken) {
    throw Error('API yanıtında accessToken bulunamadı.');
  }

  sessionStorage.setItem('authToken', data.accessToken);
  sessionStorage.setItem('authUser', JSON.stringify(data.user));

  return data;
}

export function registerUser(userData) {
  return sendAuthRequest('register', {
    username: userData.username.trim(),
    displayName: userData.displayName.trim(),
    email: userData.email.trim(),
    password: userData.password
  });
}

export function loginUser(userData) {
  return sendAuthRequest('login', {
    login: userData.login.trim(),
    password: userData.password
  });
}

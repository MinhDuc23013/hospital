import axios from 'axios';
import keycloak from '../auth/keycloak';
import { saveSession, clearSession } from '../auth/session';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5084';
const CLIENT_ID = import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'hospital-frontend';

const api = axios.create({
  baseURL: API_BASE,
  timeout: 15_000,
});

// In-flight refresh promise to avoid duplicate refresh calls
let refreshPromise: Promise<string | null> | null = null;

async function refreshAccessToken(): Promise<string | null> {
  if (refreshPromise) return refreshPromise;

  const refreshToken = keycloak.refreshToken ?? localStorage.getItem('hrm_refresh_token');
  if (!refreshToken) return null;

  refreshPromise = (async () => {
    try {
      const body = new URLSearchParams({
        grant_type: 'refresh_token',
        client_id: CLIENT_ID,
        refresh_token: refreshToken,
      });

      const { data } = await axios.post(
        `${API_BASE}/api/auth/token`,
        body,
        { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } },
      );

      saveSession(keycloak, data);
      return data.access_token as string;
    } catch (err) {
      clearSession(keycloak);
      return null;
    } finally {
      refreshPromise = null;
    }
  })();

  return refreshPromise;
}

// Attach Bearer token on every request
api.interceptors.request.use(async config => {
  if (keycloak.token) {
    config.headers['Authorization'] = `Bearer ${keycloak.token}`;
  }
  if (keycloak.subject) {
    config.headers['X-User-Id'] = keycloak.subject;
  }
  return config;
});

// Handle 401 with refresh + retry
api.interceptors.response.use(
  res => res,
  async err => {
    const original = err.config;

    if (err.response?.status === 401 && !original._retry) {
      original._retry = true;

      const newToken = await refreshAccessToken();
      if (newToken) {
        original.headers['Authorization'] = `Bearer ${newToken}`;
        return api(original);
      }

      // Refresh failed → redirect to login
      window.location.href = '/login';
    }

    return Promise.reject(err);
  },
);

export default api;

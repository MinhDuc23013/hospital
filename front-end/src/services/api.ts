import axios from 'axios';
import { useAuthStore } from '../store/authStore';

const api = axios.create({
  baseURL: '/',
  timeout: 15_000,
  withCredentials: true,  // send bff_session cookie on every request
});

// In-flight refresh promise — prevents duplicate refresh calls on concurrent 401s
let refreshPromise: Promise<boolean> | null = null;

async function refreshSession(): Promise<boolean> {
  if (refreshPromise) return refreshPromise;

  refreshPromise = (async () => {
    try {
      const res = await fetch('/bff/refresh', { method: 'POST', credentials: 'include' });
      return res.ok;
    } catch {
      return false;
    } finally {
      refreshPromise = null;
    }
  })();

  return refreshPromise;
}

// Handle 401: refresh session then retry once
api.interceptors.response.use(
  res => res,
  async err => {
    const original = err.config;

    if (err.response?.status === 401 && !original._retry) {
      original._retry = true;

      const refreshed = await refreshSession();
      if (refreshed) return api(original);

      // Refresh failed — clear store and redirect to login
      useAuthStore.getState().setUser(null);
      window.location.href = '/login';
    }

    return Promise.reject(err);
  },
);

export default api;

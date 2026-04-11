import axios from 'axios';
import keycloak from '../auth/keycloak';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5084',
  timeout: 15_000,
});

// Attach Bearer token and forwarded user ID on every request
api.interceptors.request.use(async config => {
  if (keycloak.isTokenExpired(30)) {
    await keycloak.updateToken(30);
  }
  if (keycloak.token) {
    config.headers['Authorization'] = `Bearer ${keycloak.token}`;
  }
  if (keycloak.subject) {
    config.headers['X-User-Id'] = keycloak.subject;
  }
  return config;
});

// Redirect to login on 401
api.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      keycloak.login();
    }
    return Promise.reject(err);
  },
);

export default api;

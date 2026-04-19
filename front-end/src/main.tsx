import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import keycloak from './auth/keycloak';
import { useAuthStore } from './store/authStore';
import { Providers } from './app/Providers';
import { AppRoutes } from './routes/AppRoutes';
import { restoreSession } from './auth/session';

// Restore session from localStorage if available (tokens saved after login)
const restored = restoreSession(keycloak);

// Init keycloak in "passive" mode — do NOT redirect automatically.
// Our custom LoginPage handles login via gateway AuthProxyController.
keycloak
  .init({
    // No onLoad — never auto-redirect. Auth handled entirely by our LoginPage.
    checkLoginIframe: false,
    token: restored.token,
    refreshToken: restored.refreshToken,
  })
  .then(authenticated => {
    useAuthStore.getState().setAuthenticated(authenticated || restored.authenticated);
    useAuthStore.getState().setInitialized(true);

    createRoot(document.getElementById('root')!).render(
      <StrictMode>
        <Providers>
          <AppRoutes />
        </Providers>
      </StrictMode>,
    );
  })
  .catch(err => {
    console.error('Keycloak init failed', err);
    // Render anyway — routes will show /login if not authenticated
    useAuthStore.getState().setInitialized(true);
    createRoot(document.getElementById('root')!).render(
      <StrictMode>
        <Providers>
          <AppRoutes />
        </Providers>
      </StrictMode>,
    );
  });

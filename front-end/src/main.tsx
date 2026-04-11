import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import keycloak from './auth/keycloak';
import { useAuthStore } from './store/authStore';
import { Providers } from './app/Providers';
import { AppRoutes } from './routes/AppRoutes';

keycloak
  .init({ onLoad: 'login-required', checkLoginIframe: false })
  .then(authenticated => {
    useAuthStore.getState().setAuthenticated(authenticated);
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
  });

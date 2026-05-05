import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import './index.css';
import { useAuthStore } from './store/authStore';
import { Providers } from './app/Providers';
import { AppRoutes } from './routes/AppRoutes';

// Check existing session via BFF before rendering — no token ever touches the browser
async function initAuth() {
  try {
    const res = await fetch('/bff/me', { credentials: 'include' });
    if (res.ok) {
      const user = await res.json();
      useAuthStore.getState().setUser(user);
    }
  } catch {
    // Network error — proceed unauthenticated
  } finally {
    useAuthStore.getState().setInitialized(true);
  }
}

initAuth().then(() => {
  createRoot(document.getElementById('root')!).render(
    <StrictMode>
      <Providers>
        <AppRoutes />
      </Providers>
    </StrictMode>,
  );
});

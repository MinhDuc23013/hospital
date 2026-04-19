import { useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import axios from 'axios';
import keycloak from '../../auth/keycloak';
import { saveSession } from '../../auth/session';
import { useAuthStore } from '../../store/authStore';

const KEYCLOAK_URL = import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080';
const CLIENT_ID = import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'hospital-frontend';
const REDIRECT_URI = `${window.location.origin}/auth/callback`;

export default function AuthCallbackPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const setAuthenticated = useAuthStore(s => s.setAuthenticated);

  useEffect(() => {
    const code = params.get('code');
    const error = params.get('error');

    if (error) {
      navigate(`/login?error=${encodeURIComponent(error)}`, { replace: true });
      return;
    }

    if (!code) {
      navigate('/login', { replace: true });
      return;
    }

    // Exchange authorization code for tokens
    const exchangeCode = async () => {
      try {
        const body = new URLSearchParams({
          grant_type: 'authorization_code',
          client_id: CLIENT_ID,
          code,
          redirect_uri: REDIRECT_URI,
        });

        // Call Keycloak directly (token endpoint for Authorization Code Flow)
        const { data } = await axios.post(
          `${KEYCLOAK_URL}/realms/hospital/protocol/openid-connect/token`,
          body,
          { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } },
        );

        saveSession(keycloak, data);
        setAuthenticated(true);
        navigate('/dashboard', { replace: true });
      } catch (err: any) {
        console.error('Token exchange failed', err);
        navigate('/login?error=oauth_exchange_failed', { replace: true });
      }
    };

    exchangeCode();
  }, [params, navigate, setAuthenticated]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="text-center">
        <div className="inline-block w-12 h-12 border-4 border-emerald-700 border-t-transparent rounded-full animate-spin mb-4" />
        <p className="text-gray-600">Signing you in...</p>
      </div>
    </div>
  );
}

import { useEffect } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuthStore } from '../../store/authStore';

export default function AuthCallbackPage() {
  const [params]  = useSearchParams();
  const navigate  = useNavigate();
  const setUser   = useAuthStore(s => s.setUser);

  useEffect(() => {
    const code  = params.get('code');
    const error = params.get('error');

    if (error) { navigate(`/login?error=${encodeURIComponent(error)}`, { replace: true }); return; }
    if (!code)  { navigate('/login', { replace: true }); return; }

    const exchange = async () => {
      try {
        const res = await fetch('/bff/callback', {
          method:      'POST',
          credentials: 'include',
          headers:     { 'Content-Type': 'application/json' },
          body:        JSON.stringify({ code, redirectUri: `${window.location.origin}/auth/callback` }),
        });

        if (!res.ok) throw new Error(`BFF callback failed: ${res.status}`);

        const { user } = await res.json();
        setUser(user);
        navigate('/dashboard', { replace: true });
      } catch (err) {
        console.error('Token exchange failed', err);
        navigate('/login?error=oauth_exchange_failed', { replace: true });
      }
    };

    exchange();
  }, [params, navigate, setUser]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="text-center">
        <div className="inline-block w-12 h-12 border-4 border-emerald-700 border-t-transparent rounded-full animate-spin mb-4" />
        <p className="text-gray-600">Signing you in...</p>
      </div>
    </div>
  );
}

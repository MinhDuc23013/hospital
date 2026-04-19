import { useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import axios from 'axios';
import keycloak from '../../auth/keycloak';
import { saveSession } from '../../auth/session';
import { useAuthStore } from '../../store/authStore';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5084';
const KEYCLOAK_URL = import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080';
const CLIENT_ID = import.meta.env.VITE_KEYCLOAK_CLIENT_ID ?? 'hospital-frontend';

function loginWithGoogle() {
  const redirectUri = `${window.location.origin}/auth/callback`;
  const params = new URLSearchParams({
    client_id: CLIENT_ID,
    redirect_uri: redirectUri,
    response_type: 'code',
    scope: 'openid email profile',
    kc_idp_hint: 'google',
  });
  window.location.href = `${KEYCLOAK_URL}/realms/hospital/protocol/openid-connect/auth?${params}`;
}

interface TokenResponse {
  access_token: string;
  refresh_token: string;
  expires_in: number;
  token_type: string;
}

export default function LoginPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const setAuthenticated = useAuthStore(s => s.setAuthenticated);

  // 'patient' (no MFA) or 'staff' (admin/doctor, MFA required)
  const loginAs = searchParams.get('as') === 'staff' ? 'staff' : 'patient';
  const isStaff = loginAs === 'staff';

  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [totp, setTotp] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>();
  const [retryAfter, setRetryAfter] = useState<number>();
  const [needsTotp, setNeedsTotp] = useState(false);
  const [needsTotpSetup, setNeedsTotpSetup] = useState(false);
  const [needsEmailVerification, setNeedsEmailVerification] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(undefined);
    setRetryAfter(undefined);
    setLoading(true);

    try {
      const params: Record<string, string> = {
        grant_type: 'password',
        client_id: CLIENT_ID,
        username,
        password,
      };
      if (totp) params.totp = totp;

      const { data } = await axios.post<TokenResponse>(
        `${API_BASE}/api/auth/token`,
        new URLSearchParams(params),
        { headers: { 'Content-Type': 'application/x-www-form-urlencoded' } },
      );

      saveSession(keycloak, data);
      setAuthenticated(true);
      navigate('/dashboard', { replace: true });
    } catch (err: any) {
      const status = err.response?.status;
      const body = err.response?.data;
      const keycloakError = body?.error_description || body?.error;

      if (status === 429) {
        const seconds = body?.error?.retryAfter ?? 600;
        setRetryAfter(seconds);
        setError(
          `Too many failed attempts. Try again in ${Math.ceil(seconds / 60)} minute${seconds > 60 ? 's' : ''}.`,
        );
      } else if (keycloakError?.toLowerCase().includes('not fully set up')) {
        // Account has pending required actions — most common:
        // - VERIFY_EMAIL (patient chưa verify)
        // - CONFIGURE_TOTP (staff chưa setup MFA)
        if (isStaff) {
          setNeedsTotpSetup(true);
          setError('MFA setup required. Please complete TOTP setup first.');
        } else {
          setNeedsEmailVerification(true);
          setError('Email not verified. Please check your inbox and click the verification link.');
        }
      } else if ((status === 401 || status === 400) && !totp) {
        // First attempt failed — could be wrong password OR missing TOTP (Keycloak returns same error)
        // Show TOTP input; user retries with either correct password or TOTP code
        setNeedsTotp(true);
        setError('If MFA is enabled, enter your 6-digit authenticator code. Otherwise, check your password.');
      } else if ((status === 401 || status === 400) && totp) {
        // TOTP provided but still failed — either password wrong or TOTP wrong
        setError('Invalid credentials or authenticator code');
      } else {
        setError('Login failed. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  const keycloakSetupUrl = `${import.meta.env.VITE_KEYCLOAK_URL ?? 'http://localhost:8080'}/realms/hospital/account/`;

  return (
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-br from-emerald-50 to-gray-100 p-4">
      <div className="w-full max-w-md">
        <div className="bg-white rounded-lg shadow-xl p-8">
          <div className="text-center mb-6">
            <div className="inline-flex items-center gap-2 mb-3">
              <div className={`w-10 h-10 rounded-full flex items-center justify-center text-xl ${
                isStaff ? 'bg-teal-100 text-teal-700' : 'bg-emerald-100 text-emerald-700'
              }`}>
                {isStaff ? '🏥' : '👤'}
              </div>
            </div>
            <h1 className="text-2xl font-bold text-gray-900">
              {isStaff ? 'Staff Sign In' : 'Patient Sign In'}
            </h1>
            <p className="text-sm text-gray-500 mt-1">
              {isStaff
                ? 'Admin / Doctor access — MFA required'
                : 'Access your health records'}
            </p>

            {/* Toggle between patient / staff */}
            <div className="mt-4 inline-flex bg-gray-100 rounded-lg p-1 text-xs">
              <Link
                to="/login?as=patient"
                className={`px-4 py-1.5 rounded-md font-medium transition ${
                  !isStaff ? 'bg-white shadow text-emerald-700' : 'text-gray-600 hover:text-gray-900'
                }`}
              >
                Patient
              </Link>
              <Link
                to="/login?as=staff"
                className={`px-4 py-1.5 rounded-md font-medium transition ${
                  isStaff ? 'bg-white shadow text-teal-700' : 'text-gray-600 hover:text-gray-900'
                }`}
              >
                Staff
              </Link>
            </div>
          </div>

          {error && (
            <div
              className={`mb-4 p-3 rounded text-sm border ${
                retryAfter
                  ? 'bg-yellow-50 border-yellow-200 text-yellow-800'
                  : 'bg-red-50 border-red-200 text-red-700'
              }`}
            >
              {error}
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Username
              </label>
              <input
                value={username}
                onChange={e => setUsername(e.target.value)}
                type="text"
                required
                autoFocus
                autoComplete="username"
                className="w-full border border-gray-300 rounded px-3 py-2 text-sm focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 outline-none"
                placeholder="Enter username"
              />
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Password
              </label>
              <div className="relative">
                <input
                  value={password}
                  onChange={e => setPassword(e.target.value)}
                  type={showPassword ? 'text' : 'password'}
                  required
                  autoComplete="current-password"
                  className="w-full border border-gray-300 rounded px-3 py-2 text-sm pr-20 focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 outline-none"
                  placeholder="Enter password"
                />
                <button
                  type="button"
                  onClick={() => setShowPassword(s => !s)}
                  className="absolute right-2 top-1/2 -translate-y-1/2 text-xs text-emerald-700 hover:text-emerald-900 px-2"
                >
                  {showPassword ? 'Hide' : 'Show'}
                </button>
              </div>
            </div>

            {needsTotp && (
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Authenticator Code
                </label>
                <input
                  value={totp}
                  onChange={e => setTotp(e.target.value.replace(/\D/g, '').slice(0, 6))}
                  type="text"
                  inputMode="numeric"
                  autoFocus
                  maxLength={6}
                  autoComplete="one-time-code"
                  className="w-full border border-gray-300 rounded px-3 py-2 text-sm tracking-widest text-center font-mono focus:ring-2 focus:ring-emerald-500 focus:border-emerald-500 outline-none"
                  placeholder="000000"
                />
                <p className="text-xs text-gray-500 mt-1">
                  Enter the 6-digit code from your authenticator app
                </p>
              </div>
            )}

            <button
              type="submit"
              disabled={loading || !!retryAfter}
              className={`w-full py-2 text-white rounded font-medium disabled:opacity-50 disabled:cursor-not-allowed transition-colors ${
                isStaff ? 'bg-teal-700 hover:bg-teal-800' : 'bg-emerald-700 hover:bg-emerald-800'
              }`}
            >
              {loading ? 'Signing in...' : needsTotp ? 'Verify & Sign in' : 'Sign in'}
            </button>
          </form>

          {needsEmailVerification && (
            <div className="mt-4 p-4 bg-yellow-50 border border-yellow-200 rounded text-sm text-yellow-900">
              <div className="flex items-start gap-2 mb-2">
                <span className="text-lg">📧</span>
                <p className="font-semibold">Email Verification Required</p>
              </div>
              <p className="text-xs mb-3">
                Your account <strong>{username}</strong> hasn't been verified yet.
                Please check your inbox (and spam folder) for the verification link.
              </p>
              <p className="text-xs text-yellow-700">
                Didn't receive the email? Check <a href="http://localhost:8025" target="_blank" rel="noopener noreferrer" className="underline hover:text-yellow-900">MailHog</a> (dev) or contact support.
              </p>
            </div>
          )}

          {needsTotpSetup && (
            <div className="mt-4 p-3 bg-emerald-50 border border-emerald-200 rounded text-xs text-emerald-800">
              <p className="font-medium mb-2">MFA Setup Required</p>
              <p className="mb-2">
                This account requires multi-factor authentication. Setup your authenticator app first:
              </p>
              <ol className="list-decimal list-inside space-y-1 mb-2">
                <li>Click the button below to open account settings</li>
                <li>Sign in with your password</li>
                <li>Scan QR code with Google Authenticator / Authy</li>
                <li>Return here to login with your new 6-digit code</li>
              </ol>
              <a
                href={keycloakSetupUrl}
                target="_blank"
                rel="noopener noreferrer"
                className="inline-block mt-1 px-3 py-1.5 bg-emerald-700 text-white rounded hover:bg-emerald-800"
              >
                Open MFA Setup →
              </a>
            </div>
          )}

          {/* Google login + Register link — only for patients */}
          {!isStaff && (
            <>
              <div className="my-5 flex items-center gap-3">
                <div className="flex-1 h-px bg-gray-200" />
                <span className="text-xs text-gray-400">or continue with</span>
                <div className="flex-1 h-px bg-gray-200" />
              </div>

              <button
                type="button"
                onClick={loginWithGoogle}
                className="w-full py-2 border border-gray-300 rounded font-medium text-gray-700 hover:bg-gray-50 flex items-center justify-center gap-3"
              >
                <svg width="18" height="18" viewBox="0 0 24 24">
                  <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z" />
                  <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z" />
                  <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z" />
                  <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z" />
                </svg>
                Sign in with Google
              </button>

              <div className="mt-6 text-center text-sm text-gray-600">
                Don't have an account?{' '}
                <Link to="/register" className="text-emerald-700 hover:text-emerald-900 font-medium">
                  Create one
                </Link>
              </div>
            </>
          )}

          {isStaff && (
            <div className="mt-5 p-3 bg-teal-50 border border-teal-200 rounded text-xs text-teal-800">
              <strong>🔒 MFA Required:</strong> Staff accounts must use an authenticator app.
              Contact IT if you need help with setup.
            </div>
          )}

          <div className="mt-3 text-center text-xs text-gray-500">
            Login attempts are monitored. After 5 failed attempts, your account will be temporarily locked.
          </div>
        </div>

        <div className="mt-4 text-center text-xs text-gray-400">
          © Hospital HRM System
        </div>
      </div>
    </div>
  );
}

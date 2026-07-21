import { useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { fileApi } from './file-api';

export default function DriveCallbackPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const code = params.get('code');
    if (!code) {
      setError('Missing authorization code from Google.');
      return;
    }

    fileApi
      .submitGoogleCallback(code)
      .then(() => navigate('/files', { replace: true }))
      .catch(() => setError('Failed to connect Google Drive. Please try again.'));
  }, [params, navigate]);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <div className="text-center">
        {error ? (
          <>
            <p className="text-red-600 mb-3">{error}</p>
            <Link to="/files" className="text-blue-600 hover:underline text-sm">
              Back to Files
            </Link>
          </>
        ) : (
          <>
            <div className="inline-block w-12 h-12 border-4 border-emerald-700 border-t-transparent rounded-full animate-spin mb-4" />
            <p className="text-gray-600">Connecting Google Drive...</p>
          </>
        )}
      </div>
    </div>
  );
}

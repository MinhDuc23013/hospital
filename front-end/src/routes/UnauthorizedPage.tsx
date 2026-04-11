import { useNavigate } from 'react-router-dom';

export default function UnauthorizedPage() {
  const navigate = useNavigate();
  return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-4">
      <h1 className="text-2xl font-bold text-red-600">403 — Unauthorized</h1>
      <p className="text-gray-500">You do not have permission to view this page.</p>
      <button
        onClick={() => navigate('/dashboard')}
        className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700"
      >
        Back to Dashboard
      </button>
    </div>
  );
}

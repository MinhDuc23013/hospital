import { useState } from 'react';
import { useAdminUsers, useDeleteAdminUser } from './use-admin-users';
import { UserFormModal } from './UserFormModal';
import { ResetPasswordModal } from './ResetPasswordModal';
import type { KeycloakUser } from '../../shared/types/admin';

export default function AdminUsersPage() {
  const [page, setPage] = useState(1);
  const [searchInput, setSearchInput] = useState('');
  const [appliedSearch, setAppliedSearch] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [editTarget, setEditTarget] = useState<KeycloakUser | null>(null);
  const [resetPasswordTarget, setResetPasswordTarget] = useState<KeycloakUser | null>(null);

  const [meResult, setMeResult] = useState<string | null>(null);
  const [meLoading, setMeLoading] = useState(false);

  const { data, isLoading, isError } = useAdminUsers(appliedSearch || undefined, page, 20);
  const deleteMutation = useDeleteAdminUser();

  const handleCheckMe = async () => {
    setMeLoading(true);
    setMeResult(null);
    try {
      const res = await fetch('/bff/me', { credentials: 'include' });
      const body = await res.json();
      setMeResult(JSON.stringify(body, null, 2));
    } catch (e) {
      setMeResult('Error: ' + String(e));
    } finally {
      setMeLoading(false);
    }
  };

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setAppliedSearch(searchInput.trim());
    setPage(1);
  };

  const handleDelete = (user: KeycloakUser) => {
    if (!confirm(`Delete user ${user.email}?`)) return;
    deleteMutation.mutate(user.id);
  };


  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">User Management</h1>
        <div className="flex gap-2">
          <button onClick={handleCheckMe} disabled={meLoading}
            className="px-4 py-2 bg-gray-700 text-white text-sm rounded hover:bg-gray-800 disabled:opacity-50">
            {meLoading ? 'Loading...' : 'Check /bff/me'}
          </button>
          <button onClick={() => setShowCreate(true)}
            className="px-4 py-2 bg-blue-600 text-white text-sm rounded hover:bg-blue-700">
            + Add User
          </button>
        </div>
      </div>

      {meResult && (
        <pre className="mb-4 p-3 bg-gray-900 text-green-400 text-xs rounded overflow-auto max-h-48">{meResult}</pre>
      )}

      <form onSubmit={handleSearch} className="flex gap-3 mb-6">
        <input value={searchInput} onChange={e => setSearchInput(e.target.value)}
          placeholder="Search by email or name..."
          className="border rounded px-3 py-2 text-sm w-72" />
        <button type="submit"
          className="px-4 py-2 bg-gray-100 text-gray-700 text-sm rounded hover:bg-gray-200">
          Search
        </button>
      </form>

      {isLoading && <p className="text-gray-500">Loading...</p>}
      {isError && <p className="text-red-500">Failed to load users.</p>}

      {data && (
        <>
          <div className="overflow-x-auto rounded border border-gray-200">
            <table className="w-full text-sm">
              <thead className="bg-gray-100 text-left">
                <tr>
                  <th className="px-4 py-3 font-medium">Email</th>
                  <th className="px-4 py-3 font-medium">First Name</th>
                  <th className="px-4 py-3 font-medium">Last Name</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  <th className="px-4 py-3 font-medium">Roles</th>
                  <th className="px-4 py-3 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.data.map(u => (
                  <tr key={u.id} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-3 text-gray-700">{u.email ?? '—'}</td>
                    <td className="px-4 py-3 text-gray-600">{u.firstName ?? '—'}</td>
                    <td className="px-4 py-3 text-gray-600">{u.lastName ?? '—'}</td>
                    <td className="px-4 py-3">
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                        u.enabled ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-600'
                      }`}>
                        {u.enabled ? 'Active' : 'Disabled'}
                      </span>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap gap-1">
                        {(u.roles ?? []).map(r => (
                          <span key={r} className="px-2 py-0.5 bg-blue-100 text-blue-700 text-xs rounded-full">{r}</span>
                        ))}
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex gap-2">
                        <button onClick={() => setEditTarget(u)}
                          className="text-blue-600 hover:underline text-xs">Edit</button>
                        <button onClick={() => setResetPasswordTarget(u)}
                          className="text-yellow-600 hover:underline text-xs">Reset PW</button>
                        <button onClick={() => handleDelete(u)}
                          disabled={deleteMutation.isPending}
                          className="text-red-500 hover:underline text-xs">Delete</button>
                      </div>
                    </td>
                  </tr>
                ))}
                {data.data.length === 0 && (
                  <tr>
                    <td colSpan={6} className="px-4 py-6 text-center text-gray-400">No users found.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
            <span>Total: {data.pagination.total} users</span>
            <div className="flex gap-2">
              <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}
                className="px-3 py-1 border rounded disabled:opacity-40 hover:bg-gray-100">Prev</button>
              <span className="px-3 py-1">Page {page}</span>
              <button onClick={() => setPage(p => p + 1)} disabled={page * 20 >= data.pagination.total}
                className="px-3 py-1 border rounded disabled:opacity-40 hover:bg-gray-100">Next</button>
            </div>
          </div>
        </>
      )}

      {showCreate && <UserFormModal mode="create" onClose={() => setShowCreate(false)} />}
      {editTarget && <UserFormModal mode="edit" user={editTarget} onClose={() => setEditTarget(null)} />}
      {resetPasswordTarget && (
        <ResetPasswordModal
          user={resetPasswordTarget}
          onClose={() => setResetPasswordTarget(null)}
        />
      )}
    </div>
  );
}

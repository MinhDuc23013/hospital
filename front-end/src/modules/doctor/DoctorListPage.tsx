import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useDoctors, useDeactivateDoctor } from './use-doctors';
import type { Doctor } from '../../shared/types/doctor';
import { DoctorFormModal } from './DoctorFormModal';

export default function DoctorListPage() {
  const [page, setPage] = useState(1);
  const [showCreate, setShowCreate] = useState(false);
  const [editTarget, setEditTarget] = useState<Doctor | null>(null);

  const { data, isLoading, isError } = useDoctors(undefined, undefined, page, 20);
  const deactivateMutation = useDeactivateDoctor();

  const handleDeactivate = (doctor: Doctor) => {
    if (!confirm(`Deactivate Dr. ${doctor.fullName}?`)) return;
    deactivateMutation.mutate(doctor.id);
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Doctors</h1>
        <div className="flex gap-3">
          <Link to="/doctors/schedules"
            className="px-4 py-2 bg-gray-100 text-gray-700 text-sm rounded hover:bg-gray-200">
            Schedules
          </Link>
          <button onClick={() => setShowCreate(true)}
            className="px-4 py-2 bg-blue-600 text-white text-sm rounded hover:bg-blue-700">
            + Add Doctor
          </button>
        </div>
      </div>

      {isLoading && <p className="text-gray-500">Loading...</p>}
      {isError && <p className="text-red-500">Failed to load doctors.</p>}

      {data && (
        <>
          <div className="overflow-x-auto rounded border border-gray-200">
            <table className="w-full text-sm">
              <thead className="bg-gray-100 text-left">
                <tr>
                  <th className="px-4 py-3 font-medium">Full Name</th>
                  <th className="px-4 py-3 font-medium">Specialty</th>
                  <th className="px-4 py-3 font-medium">Email</th>
                  <th className="px-4 py-3 font-medium">Phone</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  <th className="px-4 py-3 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.data.map(d => (
                  <tr key={d.id} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium">{d.fullName}</td>
                    <td className="px-4 py-3 text-gray-600">{d.specialty}</td>
                    <td className="px-4 py-3 text-gray-600">{d.email ?? '—'}</td>
                    <td className="px-4 py-3 text-gray-600">{d.phone ?? '—'}</td>
                    <td className="px-4 py-3">
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                        d.isActive ? 'bg-green-100 text-green-700' : 'bg-red-100 text-red-600'
                      }`}>
                        {d.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td className="px-4 py-3 flex gap-2">
                      <button onClick={() => setEditTarget(d)}
                        className="text-blue-600 hover:underline text-xs">Edit</button>
                      {d.isActive && (
                        <button onClick={() => handleDeactivate(d)}
                          disabled={deactivateMutation.isPending}
                          className="text-red-500 hover:underline text-xs">Deactivate</button>
                      )}
                    </td>
                  </tr>
                ))}
                {data.data.length === 0 && (
                  <tr>
                    <td colSpan={6} className="px-4 py-6 text-center text-gray-400">No doctors found.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
            <span>Total: {data.pagination.total} doctors</span>
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

      {showCreate && <DoctorFormModal mode="create" onClose={() => setShowCreate(false)} />}
      {editTarget && <DoctorFormModal mode="edit" doctor={editTarget} onClose={() => setEditTarget(null)} />}
    </div>
  );
}

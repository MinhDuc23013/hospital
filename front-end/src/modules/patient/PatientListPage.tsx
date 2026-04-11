import { useState } from 'react';
import { Link } from 'react-router-dom';
import { usePatients, useDeletePatient } from './use-patients';
import type { Patient } from '../../shared/types/patient';
import { PatientFormModal } from './PatientFormModal';

export default function PatientListPage() {
  const [page, setPage] = useState(1);
  const [editTarget, setEditTarget] = useState<Patient | null>(null);
  const [showCreate, setShowCreate] = useState(false);

  const { data, isLoading, isError } = usePatients(page, 20);
  const deleteMutation = useDeletePatient();

  const handleDelete = (patient: Patient) => {
    if (!confirm(`Delete patient ${patient.firstName} ${patient.lastName}?`)) return;
    deleteMutation.mutate(patient.id);
  };

  return (
    <div>
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Patients</h1>
        <div className="flex gap-3">
          <Link
            to="/patients/register"
            className="px-4 py-2 bg-green-600 text-white text-sm rounded hover:bg-green-700"
          >
            + Register Patient
          </Link>
          <button
            onClick={() => setShowCreate(true)}
            className="px-4 py-2 bg-blue-600 text-white text-sm rounded hover:bg-blue-700"
          >
            + Quick Add
          </button>
        </div>
      </div>

      {/* States */}
      {isLoading && <p className="text-gray-500">Loading...</p>}
      {isError && <p className="text-red-500">Failed to load patients.</p>}

      {/* Table */}
      {data && (
        <>
          <div className="overflow-x-auto rounded border border-gray-200">
            <table className="w-full text-sm">
              <thead className="bg-gray-100 text-left">
                <tr>
                  <th className="px-4 py-3 font-medium">Full Name</th>
                  <th className="px-4 py-3 font-medium">Email</th>
                  <th className="px-4 py-3 font-medium">Date of Birth</th>
                  <th className="px-4 py-3 font-medium">Phone</th>
                  <th className="px-4 py-3 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.data.map(p => (
                  <tr key={p.id} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium">
                      {p.firstName} {p.lastName}
                    </td>
                    <td className="px-4 py-3 text-gray-600">{p.email}</td>
                    <td className="px-4 py-3 text-gray-600">{p.dateOfBirth}</td>
                    <td className="px-4 py-3 text-gray-600">{p.phoneNumber ?? '—'}</td>
                    <td className="px-4 py-3 flex gap-2">
                      <button
                        onClick={() => setEditTarget(p)}
                        className="text-blue-600 hover:underline text-xs"
                      >
                        Edit
                      </button>
                      <button
                        onClick={() => handleDelete(p)}
                        className="text-red-500 hover:underline text-xs"
                        disabled={deleteMutation.isPending}
                      >
                        Delete
                      </button>
                    </td>
                  </tr>
                ))}
                {data.data.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-6 text-center text-gray-400">
                      No patients found.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          {/* Pagination */}
          <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
            <span>
              Total: {data.pagination.total} patients
            </span>
            <div className="flex gap-2">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-3 py-1 border rounded disabled:opacity-40 hover:bg-gray-100"
              >
                Prev
              </button>
              <span className="px-3 py-1">Page {page}</span>
              <button
                onClick={() => setPage(p => p + 1)}
                disabled={page * 20 >= data.pagination.total}
                className="px-3 py-1 border rounded disabled:opacity-40 hover:bg-gray-100"
              >
                Next
              </button>
            </div>
          </div>
        </>
      )}

      {/* Modals */}
      {showCreate && (
        <PatientFormModal mode="create" onClose={() => setShowCreate(false)} />
      )}
      {editTarget && (
        <PatientFormModal mode="edit" patient={editTarget} onClose={() => setEditTarget(null)} />
      )}
    </div>
  );
}

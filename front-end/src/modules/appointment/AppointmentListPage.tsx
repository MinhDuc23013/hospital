import { useState } from 'react';
import { useAppointments, useCancelAppointment, useCompleteAppointment } from './use-appointments';
import { AppointmentFormModal } from './AppointmentFormModal';
import type { Appointment } from '../../shared/types/appointment';

function StatusBadge({ status }: { status: Appointment['status'] }) {
  const cls =
    status === 'Completed' ? 'bg-green-100 text-green-700'
    : status === 'Cancelled' ? 'bg-red-100 text-red-600'
    : 'bg-blue-100 text-blue-700';
  return (
    <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${cls}`}>{status}</span>
  );
}

export default function AppointmentListPage() {
  const [page, setPage] = useState(1);
  const [showCreate, setShowCreate] = useState(false);

  const { data, isLoading, isError } = useAppointments(undefined, undefined, page, 20);
  const cancelMutation = useCancelAppointment();
  const completeMutation = useCompleteAppointment();

  const handleCancel = (id: string) => {
    if (!confirm('Cancel this appointment?')) return;
    cancelMutation.mutate(id);
  };

  const handleComplete = (id: string) => {
    if (!confirm('Mark this appointment as completed?')) return;
    completeMutation.mutate(id);
  };

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Appointments</h1>
        <button onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-blue-600 text-white text-sm rounded hover:bg-blue-700">
          + Create Appointment
        </button>
      </div>

      {isLoading && <p className="text-gray-500">Loading...</p>}
      {isError && <p className="text-red-500">Failed to load appointments.</p>}

      {data && (
        <>
          <div className="overflow-x-auto rounded border border-gray-200">
            <table className="w-full text-sm">
              <thead className="bg-gray-100 text-left">
                <tr>
                  <th className="px-4 py-3 font-medium">Patient ID</th>
                  <th className="px-4 py-3 font-medium">Doctor ID</th>
                  <th className="px-4 py-3 font-medium">Scheduled Time</th>
                  <th className="px-4 py-3 font-medium">Duration</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  <th className="px-4 py-3 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.data.map(a => (
                  <tr key={a.id} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-3 text-gray-600 font-mono text-xs">{a.patientId.slice(0, 8)}…</td>
                    <td className="px-4 py-3 text-gray-600 font-mono text-xs">{a.doctorId.slice(0, 8)}…</td>
                    <td className="px-4 py-3 text-gray-600">
                      {new Date(a.scheduledTime).toLocaleString('vi-VN')}
                    </td>
                    <td className="px-4 py-3 text-gray-600">{a.durationMinutes} min</td>
                    <td className="px-4 py-3"><StatusBadge status={a.status} /></td>
                    <td className="px-4 py-3 flex gap-2">
                      {a.status === 'Scheduled' && (
                        <>
                          <button onClick={() => handleComplete(a.id)}
                            disabled={completeMutation.isPending}
                            className="text-green-600 hover:underline text-xs">Complete</button>
                          <button onClick={() => handleCancel(a.id)}
                            disabled={cancelMutation.isPending}
                            className="text-red-500 hover:underline text-xs">Cancel</button>
                        </>
                      )}
                    </td>
                  </tr>
                ))}
                {data.data.length === 0 && (
                  <tr>
                    <td colSpan={6} className="px-4 py-6 text-center text-gray-400">No appointments found.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
            <span>Total: {data.pagination.total} appointments</span>
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

      {showCreate && <AppointmentFormModal onClose={() => setShowCreate(false)} />}
    </div>
  );
}

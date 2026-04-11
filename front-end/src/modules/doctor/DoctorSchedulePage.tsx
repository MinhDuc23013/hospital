import { useState } from 'react';
import { useDoctors, useDoctorSchedules } from './use-doctors';
import { CreateScheduleModal } from './CreateScheduleModal';

export default function DoctorSchedulePage() {
  const [selectedDoctorId, setSelectedDoctorId] = useState<string>('');
  const [page, setPage] = useState(1);
  const [showCreate, setShowCreate] = useState(false);

  const { data: doctorsData } = useDoctors(undefined, true, 1, 100);
  const { data, isLoading, isError } = useDoctorSchedules(
    selectedDoctorId || undefined, undefined, page, 20
  );

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold">Doctor Schedules</h1>
        <button onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-blue-600 text-white text-sm rounded hover:bg-blue-700">
          + Add Schedule
        </button>
      </div>

      <div className="mb-4">
        <select value={selectedDoctorId} onChange={e => { setSelectedDoctorId(e.target.value); setPage(1); }}
          className="border rounded px-3 py-2 text-sm w-64">
          <option value="">All Doctors</option>
          {doctorsData?.data.map(d => (
            <option key={d.id} value={d.id}>{d.fullName} — {d.specialty}</option>
          ))}
        </select>
      </div>

      {isLoading && <p className="text-gray-500">Loading...</p>}
      {isError && <p className="text-red-500">Failed to load schedules.</p>}

      {data && (
        <>
          <div className="overflow-x-auto rounded border border-gray-200">
            <table className="w-full text-sm">
              <thead className="bg-gray-100 text-left">
                <tr>
                  <th className="px-4 py-3 font-medium">Doctor</th>
                  <th className="px-4 py-3 font-medium">Date</th>
                  <th className="px-4 py-3 font-medium">Start</th>
                  <th className="px-4 py-3 font-medium">End</th>
                  <th className="px-4 py-3 font-medium">Slot (min)</th>
                  <th className="px-4 py-3 font-medium">Available / Total</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                </tr>
              </thead>
              <tbody>
                {data.data.map(s => (
                  <tr key={s.id} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium">{s.doctorName}</td>
                    <td className="px-4 py-3 text-gray-600">{s.date}</td>
                    <td className="px-4 py-3 text-gray-600">{s.startTime}</td>
                    <td className="px-4 py-3 text-gray-600">{s.endTime}</td>
                    <td className="px-4 py-3 text-gray-600">{s.slotDurationMinutes}</td>
                    <td className="px-4 py-3 text-gray-600">{s.availableSlots} / {s.totalSlots}</td>
                    <td className="px-4 py-3">
                      <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${
                        s.status === 'Active' ? 'bg-green-100 text-green-700'
                        : s.status === 'Full' ? 'bg-yellow-100 text-yellow-700'
                        : 'bg-gray-100 text-gray-600'
                      }`}>
                        {s.status}
                      </span>
                    </td>
                  </tr>
                ))}
                {data.data.length === 0 && (
                  <tr>
                    <td colSpan={7} className="px-4 py-6 text-center text-gray-400">No schedules found.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
            <span>Total: {data.pagination.total} schedules</span>
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

      {showCreate && <CreateScheduleModal onClose={() => setShowCreate(false)} />}
    </div>
  );
}

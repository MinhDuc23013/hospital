import { useState } from 'react';
import { useMedicalRecordsByPatient } from './use-medical-records';
import type { MedicalRecord } from '../../shared/types/medical-record';

function RecordRow({ record }: { record: MedicalRecord }) {
  const [expanded, setExpanded] = useState(false);

  return (
    <>
      <tr
        className="border-t border-gray-100 hover:bg-gray-50 cursor-pointer"
        onClick={() => setExpanded(e => !e)}
      >
        <td className="px-4 py-3 font-mono text-xs text-gray-600">{record.appointmentId.slice(0, 8)}…</td>
        <td className="px-4 py-3 text-gray-700 max-w-xs truncate">{record.findings || '—'}</td>
        <td className="px-4 py-3">
          <div className="flex flex-wrap gap-1">
            {record.diagnosis.slice(0, 3).map((d, i) => (
              <span key={i} className="px-2 py-0.5 bg-blue-100 text-blue-700 text-xs rounded-full">{d}</span>
            ))}
            {record.diagnosis.length > 3 && (
              <span className="text-xs text-gray-400">+{record.diagnosis.length - 3}</span>
            )}
          </div>
        </td>
        <td className="px-4 py-3 text-gray-600 text-xs">
          {new Date(record.createdAt).toLocaleString('vi-VN')}
        </td>
        <td className="px-4 py-3 text-blue-500 text-xs">{expanded ? '▲' : '▼'}</td>
      </tr>

      {expanded && (
        <tr className="bg-gray-50">
          <td colSpan={5} className="px-6 py-4">
            <div className="space-y-3">
              <div>
                <p className="text-xs font-semibold text-gray-500 uppercase mb-1">Findings</p>
                <p className="text-sm text-gray-700">{record.findings || 'None'}</p>
              </div>
              <div>
                <p className="text-xs font-semibold text-gray-500 uppercase mb-1">Diagnosis</p>
                <div className="flex flex-wrap gap-1">
                  {record.diagnosis.map((d, i) => (
                    <span key={i} className="px-2 py-0.5 bg-blue-100 text-blue-700 text-xs rounded-full">{d}</span>
                  ))}
                </div>
              </div>
              {record.labResults.length > 0 && (
                <div>
                  <p className="text-xs font-semibold text-gray-500 uppercase mb-2">Lab Results</p>
                  <table className="w-full text-xs border rounded">
                    <thead className="bg-gray-100">
                      <tr>
                        <th className="px-3 py-2 text-left">Test</th>
                        <th className="px-3 py-2 text-left">Result</th>
                        <th className="px-3 py-2 text-left">Normal Range</th>
                        <th className="px-3 py-2 text-left">Timestamp</th>
                      </tr>
                    </thead>
                    <tbody>
                      {record.labResults.map((lr, i) => (
                        <tr key={i} className="border-t">
                          <td className="px-3 py-2">{lr.testName}</td>
                          <td className="px-3 py-2">{lr.result ?? '—'}</td>
                          <td className="px-3 py-2">{lr.normalRange ?? '—'}</td>
                          <td className="px-3 py-2">{new Date(lr.timestamp).toLocaleString('vi-VN')}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </div>
          </td>
        </tr>
      )}
    </>
  );
}

export default function MedicalRecordsPage() {
  const [patientIdInput, setPatientIdInput] = useState('');
  const [searchPatientId, setSearchPatientId] = useState('');
  const [page, setPage] = useState(1);

  const { data, isLoading, isError } = useMedicalRecordsByPatient(searchPatientId, page, 20);

  const handleSearch = (e: React.FormEvent) => {
    e.preventDefault();
    setSearchPatientId(patientIdInput.trim());
    setPage(1);
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Medical Records</h1>

      <form onSubmit={handleSearch} className="flex gap-3 mb-6">
        <input
          value={patientIdInput}
          onChange={e => setPatientIdInput(e.target.value)}
          placeholder="Enter Patient ID..."
          className="border rounded px-3 py-2 text-sm w-80"
        />
        <button type="submit"
          className="px-4 py-2 bg-blue-600 text-white text-sm rounded hover:bg-blue-700">
          Search
        </button>
      </form>

      {!searchPatientId && (
        <p className="text-gray-400 text-sm">Enter a patient ID above to view records.</p>
      )}

      {searchPatientId && isLoading && <p className="text-gray-500">Loading...</p>}
      {searchPatientId && isError && (
        <div className="p-3 bg-red-50 border border-red-200 rounded text-sm text-red-600">
          Failed to load records.
        </div>
      )}

      {data && (
        <>
          <div className="overflow-x-auto rounded border border-gray-200">
            <table className="w-full text-sm">
              <thead className="bg-gray-100 text-left">
                <tr>
                  <th className="px-4 py-3 font-medium">Appointment ID</th>
                  <th className="px-4 py-3 font-medium">Findings</th>
                  <th className="px-4 py-3 font-medium">Diagnosis</th>
                  <th className="px-4 py-3 font-medium">Created At</th>
                  <th className="px-4 py-3 font-medium"></th>
                </tr>
              </thead>
              <tbody>
                {data.data.map(r => <RecordRow key={r.id} record={r} />)}
                {data.data.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-6 text-center text-gray-400">
                      No records found for this patient.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
            <span>Total: {data.pagination.total} records</span>
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
    </div>
  );
}

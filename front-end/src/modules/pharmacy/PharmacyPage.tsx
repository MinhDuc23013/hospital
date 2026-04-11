import { useState } from 'react';
import { useDrugs, useDispensePrescription, usePrescriptionLookup } from './use-pharmacy';
import { DrugFormModal } from './DrugFormModal';
import type { Drug } from '../../shared/types/pharmacy';

const formatVND = (amount: number) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);

function DrugsTab() {
  const [page, setPage] = useState(1);
  const [showCreate, setShowCreate] = useState(false);
  const [editTarget, setEditTarget] = useState<Drug | null>(null);

  const { data, isLoading, isError } = useDrugs(undefined, undefined, page, 20);

  return (
    <div>
      <div className="flex justify-end mb-4">
        <button onClick={() => setShowCreate(true)}
          className="px-4 py-2 bg-blue-600 text-white text-sm rounded hover:bg-blue-700">
          + Add Drug
        </button>
      </div>

      {isLoading && <p className="text-gray-500">Loading...</p>}
      {isError && <p className="text-red-500">Failed to load drugs.</p>}

      {data && (
        <>
          <div className="overflow-x-auto rounded border border-gray-200">
            <table className="w-full text-sm">
              <thead className="bg-gray-100 text-left">
                <tr>
                  <th className="px-4 py-3 font-medium">Name</th>
                  <th className="px-4 py-3 font-medium">Code</th>
                  <th className="px-4 py-3 font-medium">Dosage</th>
                  <th className="px-4 py-3 font-medium">Quantity</th>
                  <th className="px-4 py-3 font-medium">Price</th>
                  <th className="px-4 py-3 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.data.map(d => (
                  <tr key={d.id} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium">{d.name}</td>
                    <td className="px-4 py-3 text-gray-600 font-mono text-xs">{d.code}</td>
                    <td className="px-4 py-3 text-gray-600">{d.dosage ?? '—'}</td>
                    <td className="px-4 py-3">
                      <span className={d.quantity <= d.lowStockThreshold ? 'text-red-600 font-semibold' : 'text-gray-700'}>
                        {d.quantity}
                        {d.quantity <= d.lowStockThreshold && (
                          <span className="ml-1 text-xs bg-red-100 text-red-600 px-1 rounded">Low</span>
                        )}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-gray-600">{formatVND(d.price)}</td>
                    <td className="px-4 py-3">
                      <button onClick={() => setEditTarget(d)}
                        className="text-blue-600 hover:underline text-xs">Edit</button>
                    </td>
                  </tr>
                ))}
                {data.data.length === 0 && (
                  <tr>
                    <td colSpan={6} className="px-4 py-6 text-center text-gray-400">No drugs found.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
            <span>Total: {data.pagination.total} drugs</span>
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

      {showCreate && <DrugFormModal mode="create" onClose={() => setShowCreate(false)} />}
      {editTarget && <DrugFormModal mode="edit" drug={editTarget} onClose={() => setEditTarget(null)} />}
    </div>
  );
}

function PrescriptionsTab() {
  const [idInput, setIdInput] = useState('');
  const [lookupId, setLookupId] = useState('');
  const dispenseMutation = useDispensePrescription();
  const { data, isLoading, isError } = usePrescriptionLookup(lookupId);

  const statusBadge = (status: string) => {
    const cls = status === 'Dispensed' ? 'bg-green-100 text-green-700'
      : status === 'Cancelled' ? 'bg-red-100 text-red-600'
      : 'bg-yellow-100 text-yellow-700';
    return <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${cls}`}>{status}</span>;
  };

  return (
    <div>
      <form onSubmit={e => { e.preventDefault(); setLookupId(idInput.trim()); }} className="flex gap-3 mb-6">
        <input value={idInput} onChange={e => setIdInput(e.target.value)}
          placeholder="Enter Prescription ID..."
          className="border rounded px-3 py-2 text-sm w-80" />
        <button type="submit" className="px-4 py-2 bg-blue-600 text-white text-sm rounded hover:bg-blue-700">
          Lookup
        </button>
      </form>

      {lookupId && isLoading && <p className="text-gray-500">Loading...</p>}
      {lookupId && isError && (
        <div className="p-3 bg-red-50 border border-red-200 rounded text-sm text-red-600">
          Prescription not found.
        </div>
      )}

      {data && (
        <div className="border rounded-lg p-5 max-w-lg bg-gray-50">
          <div className="flex items-center justify-between mb-4">
            <h3 className="font-semibold text-gray-800">Prescription Details</h3>
            {statusBadge(data.status)}
          </div>
          <dl className="grid grid-cols-2 gap-x-4 gap-y-2 text-sm">
            <dt className="text-gray-500">Drug</dt><dd className="font-medium">{data.drugName}</dd>
            <dt className="text-gray-500">Quantity</dt><dd>{data.quantity}</dd>
            <dt className="text-gray-500">Instructions</dt><dd>{data.instructions}</dd>
            <dt className="text-gray-500">Issued At</dt>
            <dd>{new Date(data.issuedAt).toLocaleString('vi-VN')}</dd>
          </dl>
          {data.status === 'Pending' && (
            <button
              onClick={() => dispenseMutation.mutate(data.id)}
              disabled={dispenseMutation.isPending}
              className="mt-4 px-4 py-2 bg-green-600 text-white text-sm rounded hover:bg-green-700 disabled:opacity-50">
              {dispenseMutation.isPending ? 'Dispensing...' : 'Dispense'}
            </button>
          )}
        </div>
      )}
    </div>
  );
}

export default function PharmacyPage() {
  const [tab, setTab] = useState<'drugs' | 'prescriptions'>('drugs');

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Pharmacy</h1>

      <div className="flex border-b mb-6">
        {(['drugs', 'prescriptions'] as const).map(t => (
          <button key={t} onClick={() => setTab(t)}
            className={`px-5 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${
              tab === t ? 'border-blue-600 text-blue-600' : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}>
            {t === 'drugs' ? 'Drugs' : 'Prescriptions'}
          </button>
        ))}
      </div>

      {tab === 'drugs' ? <DrugsTab /> : <PrescriptionsTab />}
    </div>
  );
}

import { useState } from 'react';
import { usePayments, useProcessPayment, useRefundPayment } from './use-payments';
import type { Payment } from '../../shared/types/payment';

const formatVND = (amount: number) =>
  new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(amount);

function StatusBadge({ status }: { status: Payment['status'] }) {
  const cls =
    status === 'Completed' ? 'bg-green-100 text-green-700'
    : status === 'Refunded' ? 'bg-gray-100 text-gray-600'
    : 'bg-yellow-100 text-yellow-700';
  return <span className={`px-2 py-0.5 rounded-full text-xs font-medium ${cls}`}>{status}</span>;
}

export default function BillingPage() {
  const [page, setPage] = useState(1);
  const [patientIdFilter, setPatientIdFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [appliedPatientId, setAppliedPatientId] = useState('');
  const [appliedStatus, setAppliedStatus] = useState('');

  const { data, isLoading, isError } = usePayments(
    undefined, appliedPatientId || undefined, appliedStatus || undefined, page, 20
  );
  const processMutation = useProcessPayment();
  const refundMutation = useRefundPayment();

  const handleFilter = (e: React.FormEvent) => {
    e.preventDefault();
    setAppliedPatientId(patientIdFilter.trim());
    setAppliedStatus(statusFilter);
    setPage(1);
  };

  const handleProcess = (id: string) => {
    if (!confirm('Process this payment?')) return;
    processMutation.mutate(id);
  };

  const handleRefund = (id: string) => {
    if (!confirm('Refund this payment?')) return;
    refundMutation.mutate(id);
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Billing</h1>

      <form onSubmit={handleFilter} className="flex flex-wrap gap-3 mb-6">
        <input
          value={patientIdFilter}
          onChange={e => setPatientIdFilter(e.target.value)}
          placeholder="Filter by Patient ID..."
          className="border rounded px-3 py-2 text-sm w-64"
        />
        <select
          value={statusFilter}
          onChange={e => setStatusFilter(e.target.value)}
          className="border rounded px-3 py-2 text-sm"
        >
          <option value="">All Statuses</option>
          <option value="Pending">Pending</option>
          <option value="Completed">Completed</option>
          <option value="Refunded">Refunded</option>
        </select>
        <button type="submit"
          className="px-4 py-2 bg-blue-600 text-white text-sm rounded hover:bg-blue-700">
          Filter
        </button>
      </form>

      {isLoading && <p className="text-gray-500">Loading...</p>}
      {isError && <p className="text-red-500">Failed to load payments.</p>}

      {data && (
        <>
          <div className="overflow-x-auto rounded border border-gray-200">
            <table className="w-full text-sm">
              <thead className="bg-gray-100 text-left">
                <tr>
                  <th className="px-4 py-3 font-medium">Amount</th>
                  <th className="px-4 py-3 font-medium">Method</th>
                  <th className="px-4 py-3 font-medium">Status</th>
                  <th className="px-4 py-3 font-medium">Paid At</th>
                  <th className="px-4 py-3 font-medium">Actions</th>
                </tr>
              </thead>
              <tbody>
                {data.data.map(p => (
                  <tr key={p.id} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-4 py-3 font-medium">{formatVND(p.amount)}</td>
                    <td className="px-4 py-3 text-gray-600">{p.method}</td>
                    <td className="px-4 py-3"><StatusBadge status={p.status} /></td>
                    <td className="px-4 py-3 text-gray-600 text-xs">
                      {p.paidAt ? new Date(p.paidAt).toLocaleString('vi-VN') : '—'}
                    </td>
                    <td className="px-4 py-3 flex gap-2">
                      {p.status === 'Pending' && (
                        <button onClick={() => handleProcess(p.id)}
                          disabled={processMutation.isPending}
                          className="text-green-600 hover:underline text-xs">Process</button>
                      )}
                      {p.status === 'Completed' && (
                        <button onClick={() => handleRefund(p.id)}
                          disabled={refundMutation.isPending}
                          className="text-red-500 hover:underline text-xs">Refund</button>
                      )}
                    </td>
                  </tr>
                ))}
                {data.data.length === 0 && (
                  <tr>
                    <td colSpan={5} className="px-4 py-6 text-center text-gray-400">No payments found.</td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
            <span>Total: {data.pagination.total} payments</span>
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

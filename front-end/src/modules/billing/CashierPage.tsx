import { useState, useMemo } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  WalletOutlined,
  LoginOutlined,
  LogoutOutlined,
  DollarOutlined,
  PrinterOutlined,
  ClockCircleOutlined,
  CheckCircleOutlined,
} from '@ant-design/icons';
import { paymentApi } from './payment-api';
import { useAuth } from '../../auth/useAuth';
import type { Payment, CashSession } from '../../shared/types/payment';

const formatVND = (n: number) => new Intl.NumberFormat('vi-VN').format(n) + ' ₫';

export default function CashierPage() {
  const { userId, fullName } = useAuth();
  const qc = useQueryClient();

  // Current open session for this cashier
  const { data: session, isLoading: sessionLoading } = useQuery({
    queryKey: ['cash-session', userId],
    queryFn: () => paymentApi.getCurrentCashSession(userId!),
    enabled: !!userId,
  });

  // Pending cash payments list
  const { data: payments } = useQuery({
    queryKey: ['payments', 'pending-cash'],
    queryFn: () => paymentApi.list(undefined, undefined, 'Pending', 1, 50),
  });

  const pendingCashPayments = useMemo(
    () => (payments?.data ?? []).filter(p => p.method === 'Cash' && p.status === 'Pending'),
    [payments],
  );

  const [paymentToCollect, setPaymentToCollect] = useState<Payment | null>(null);
  const [lastReceipt, setLastReceipt] = useState<Payment | null>(null);

  return (
    <div>
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 flex items-center gap-2">
            <WalletOutlined className="text-emerald-700" /> Cashier
          </h1>
          <p className="text-sm text-gray-500 mt-1">Cash payment station</p>
        </div>
      </div>

      {/* Session status card */}
      {sessionLoading ? (
        <div className="p-6 bg-white rounded-xl border">Loading session...</div>
      ) : !session ? (
        <OpenSessionCard cashierId={userId!} cashierName={fullName || 'Unknown'} onOpened={() => qc.invalidateQueries({ queryKey: ['cash-session'] })} />
      ) : (
        <SessionInfoCard session={session} onClosed={() => qc.invalidateQueries({ queryKey: ['cash-session'] })} />
      )}

      {/* Pending payments — only usable when session open */}
      {session && (
        <div className="mt-6 bg-white rounded-xl border border-gray-200 overflow-hidden">
          <div className="px-5 py-3 border-b border-gray-200 flex items-center justify-between">
            <h2 className="font-semibold text-gray-900 flex items-center gap-2">
              <ClockCircleOutlined /> Pending Cash Payments
              <span className="text-xs font-normal text-gray-500 ml-2">
                ({pendingCashPayments.length})
              </span>
            </h2>
          </div>
          {pendingCashPayments.length === 0 ? (
            <div className="p-8 text-center text-sm text-gray-400">No pending cash payments</div>
          ) : (
            <table className="w-full text-sm">
              <thead className="bg-gray-50 text-left">
                <tr>
                  <th className="px-5 py-2 font-medium">Appointment</th>
                  <th className="px-5 py-2 font-medium">Amount</th>
                  <th className="px-5 py-2 font-medium">Created</th>
                  <th className="px-5 py-2 font-medium">Action</th>
                </tr>
              </thead>
              <tbody>
                {pendingCashPayments.map(p => (
                  <tr key={p.id} className="border-t border-gray-100 hover:bg-gray-50">
                    <td className="px-5 py-3 font-mono text-xs text-gray-600">{p.appointmentId.slice(0, 8)}…</td>
                    <td className="px-5 py-3 font-semibold">{formatVND(p.amount)}</td>
                    <td className="px-5 py-3 text-gray-500">
                      {new Date(p.createdAt).toLocaleString('vi-VN')}
                    </td>
                    <td className="px-5 py-3">
                      <button
                        onClick={() => setPaymentToCollect(p)}
                        className="px-3 py-1.5 bg-emerald-700 text-white rounded text-xs font-medium hover:bg-emerald-800 flex items-center gap-1.5"
                      >
                        <DollarOutlined /> Collect Cash
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {/* Collect cash modal */}
      {paymentToCollect && session && (
        <CollectCashModal
          payment={paymentToCollect}
          sessionId={session.id}
          cashierId={userId!}
          onClose={() => setPaymentToCollect(null)}
          onCompleted={p => {
            setLastReceipt(p);
            setPaymentToCollect(null);
            qc.invalidateQueries({ queryKey: ['payments'] });
            qc.invalidateQueries({ queryKey: ['cash-session'] });
          }}
        />
      )}

      {/* Receipt modal */}
      {lastReceipt && (
        <ReceiptModal payment={lastReceipt} onClose={() => setLastReceipt(null)} />
      )}
    </div>
  );
}

function OpenSessionCard({ cashierId, cashierName, onOpened }: {
  cashierId: string; cashierName: string; onOpened: () => void;
}) {
  const [openingBalance, setOpeningBalance] = useState('500000');
  const [counterId, setCounterId] = useState('Counter-1');

  const mutation = useMutation({
    mutationFn: () =>
      paymentApi.openCashSession({
        cashierId,
        cashierName,
        counterId,
        openingBalance: Number(openingBalance),
      }),
    onSuccess: onOpened,
  });

  return (
    <div className="bg-gradient-to-br from-emerald-700 to-emerald-900 text-white rounded-xl p-6">
      <h2 className="text-lg font-semibold mb-1">No Active Session</h2>
      <p className="text-emerald-100 text-sm mb-5">Open a cash session to start collecting payments.</p>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-3 mb-4">
        <div>
          <label className="text-xs text-emerald-200 block mb-1">Counter</label>
          <input
            value={counterId}
            onChange={e => setCounterId(e.target.value)}
            className="w-full bg-white/10 border border-white/30 rounded px-3 py-2 text-sm focus:outline-none focus:bg-white/20"
          />
        </div>
        <div>
          <label className="text-xs text-emerald-200 block mb-1">Opening Balance (₫)</label>
          <input
            value={openingBalance}
            onChange={e => setOpeningBalance(e.target.value.replace(/\D/g, ''))}
            className="w-full bg-white/10 border border-white/30 rounded px-3 py-2 text-sm focus:outline-none focus:bg-white/20"
          />
        </div>
      </div>

      <button
        onClick={() => mutation.mutate()}
        disabled={mutation.isPending}
        className="px-5 py-2 bg-white text-emerald-800 rounded font-medium hover:bg-emerald-50 disabled:opacity-50 flex items-center gap-2"
      >
        <LoginOutlined /> {mutation.isPending ? 'Opening...' : 'Open Session'}
      </button>

      {mutation.error && (
        <p className="mt-3 text-xs text-red-200">
          {(mutation.error as any)?.response?.data?.error?.message || 'Failed to open session'}
        </p>
      )}
    </div>
  );
}

function SessionInfoCard({ session, onClosed }: { session: CashSession; onClosed: () => void }) {
  const [showClose, setShowClose] = useState(false);
  const [actualCash, setActualCash] = useState('');
  const [notes, setNotes] = useState('');

  const mutation = useMutation({
    mutationFn: () =>
      paymentApi.closeCashSession(session.id, { actualCash: Number(actualCash), notes: notes || undefined }),
    onSuccess: () => { setShowClose(false); onClosed(); },
  });

  const variance = actualCash ? Number(actualCash) - session.expectedCash : null;

  return (
    <div className="bg-white rounded-xl border border-emerald-200 p-6">
      <div className="flex items-start justify-between mb-4">
        <div>
          <h2 className="text-lg font-semibold text-gray-900">Active Session</h2>
          <p className="text-xs text-gray-500 mt-0.5">
            {session.counterId} · Opened {new Date(session.openedAt).toLocaleString('vi-VN')}
          </p>
        </div>
        <span className="px-2 py-1 bg-emerald-100 text-emerald-800 text-xs font-semibold rounded-full">
          OPEN
        </span>
      </div>

      <div className="grid grid-cols-2 gap-4 mb-5">
        <div>
          <div className="text-xs text-gray-500 uppercase">Opening Balance</div>
          <div className="text-xl font-bold text-gray-800">{formatVND(session.openingBalance)}</div>
        </div>
        <div>
          <div className="text-xs text-gray-500 uppercase">Expected Cash</div>
          <div className="text-xl font-bold text-emerald-700">{formatVND(session.expectedCash)}</div>
        </div>
      </div>

      {!showClose ? (
        <button
          onClick={() => setShowClose(true)}
          className="px-4 py-2 border border-gray-300 rounded text-sm font-medium hover:bg-gray-50 flex items-center gap-2"
        >
          <LogoutOutlined /> Close Session
        </button>
      ) : (
        <div className="border-t pt-4">
          <label className="text-sm font-medium block mb-1">Actual Counted Cash</label>
          <input
            value={actualCash}
            onChange={e => setActualCash(e.target.value.replace(/\D/g, ''))}
            placeholder="Enter actual amount counted"
            className="w-full border rounded px-3 py-2 text-sm mb-3"
          />
          {variance !== null && (
            <div className={`mb-3 p-2 rounded text-xs ${
              variance === 0 ? 'bg-emerald-50 text-emerald-800' :
              variance > 0 ? 'bg-amber-50 text-amber-800' : 'bg-red-50 text-red-800'
            }`}>
              Variance: <strong>{variance > 0 ? '+' : ''}{formatVND(variance)}</strong>
              {variance === 0 && ' ✓ Balanced'}
              {variance > 0 && ' (excess)'}
              {variance < 0 && ' (shortage)'}
            </div>
          )}
          <textarea
            value={notes}
            onChange={e => setNotes(e.target.value)}
            placeholder="Notes (optional)"
            rows={2}
            className="w-full border rounded px-3 py-2 text-sm mb-3 resize-none"
          />
          <div className="flex gap-2">
            <button
              onClick={() => setShowClose(false)}
              className="px-4 py-2 border rounded text-sm hover:bg-gray-50"
            >
              Cancel
            </button>
            <button
              onClick={() => mutation.mutate()}
              disabled={!actualCash || mutation.isPending}
              className="px-4 py-2 bg-red-600 text-white rounded text-sm font-medium hover:bg-red-700 disabled:opacity-50"
            >
              {mutation.isPending ? 'Closing...' : 'Confirm Close'}
            </button>
          </div>
        </div>
      )}
    </div>
  );
}

function CollectCashModal({ payment, sessionId, cashierId, onClose, onCompleted }: {
  payment: Payment; sessionId: string; cashierId: string;
  onClose: () => void; onCompleted: (p: Payment) => void;
}) {
  const [amountReceived, setAmountReceived] = useState('');
  const received = Number(amountReceived) || 0;
  const change = received - payment.amount;

  const mutation = useMutation({
    mutationFn: () =>
      paymentApi.completeCash(payment.id, {
        amountReceived: received,
        cashierId,
        cashSessionId: sessionId,
      }),
    onSuccess: onCompleted,
  });

  const quickAmounts = [payment.amount, payment.amount + 50_000, payment.amount + 100_000, payment.amount + 500_000];

  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-md p-6">
        <h2 className="text-lg font-semibold mb-1">Collect Cash Payment</h2>
        <p className="text-xs text-gray-500 mb-4">Appointment: {payment.appointmentId}</p>

        <div className="bg-emerald-50 rounded p-4 mb-4">
          <div className="text-xs text-emerald-700 uppercase mb-1">Amount Due</div>
          <div className="text-3xl font-bold text-emerald-800">{formatVND(payment.amount)}</div>
        </div>

        <label className="block text-sm font-medium mb-1">Amount Received</label>
        <input
          value={amountReceived}
          onChange={e => setAmountReceived(e.target.value.replace(/\D/g, ''))}
          placeholder="Enter amount..."
          autoFocus
          className="w-full border rounded px-3 py-2 text-lg font-mono mb-2"
        />

        <div className="flex flex-wrap gap-2 mb-4">
          {quickAmounts.map(a => (
            <button
              key={a}
              type="button"
              onClick={() => setAmountReceived(String(a))}
              className="px-3 py-1 bg-gray-100 text-xs rounded hover:bg-gray-200"
            >
              {formatVND(a)}
            </button>
          ))}
        </div>

        {received > 0 && (
          <div className={`p-3 rounded mb-4 ${
            change >= 0 ? 'bg-emerald-50 border border-emerald-200' : 'bg-red-50 border border-red-200'
          }`}>
            <div className="text-xs uppercase text-gray-600 mb-0.5">Change to Return</div>
            <div className={`text-2xl font-bold ${change >= 0 ? 'text-emerald-700' : 'text-red-600'}`}>
              {change >= 0 ? formatVND(change) : `Short ${formatVND(-change)}`}
            </div>
          </div>
        )}

        {mutation.error && (
          <div className="mb-4 p-2 bg-red-50 border border-red-200 rounded text-xs text-red-600">
            {(mutation.error as any)?.response?.data?.error?.message || 'Payment failed'}
          </div>
        )}

        <div className="flex justify-end gap-2">
          <button onClick={onClose} className="px-4 py-2 border rounded text-sm hover:bg-gray-50">
            Cancel
          </button>
          <button
            onClick={() => mutation.mutate()}
            disabled={received < payment.amount || mutation.isPending}
            className="px-4 py-2 bg-emerald-700 text-white rounded text-sm font-medium hover:bg-emerald-800 disabled:opacity-50 flex items-center gap-2"
          >
            <CheckCircleOutlined /> {mutation.isPending ? 'Processing...' : 'Confirm & Print Receipt'}
          </button>
        </div>
      </div>
    </div>
  );
}

function ReceiptModal({ payment, onClose }: { payment: Payment; onClose: () => void }) {
  return (
    <div className="fixed inset-0 bg-black/50 flex items-center justify-center z-50 p-4 print:relative print:bg-white print:p-0">
      <div className="bg-white rounded-lg shadow-xl w-full max-w-sm p-6 print:shadow-none print:max-w-full">
        <div className="text-center mb-4 pb-4 border-b border-dashed border-gray-300">
          <h2 className="text-xl font-bold">HOSPITAL HRM</h2>
          <p className="text-xs text-gray-500">Payment Receipt</p>
        </div>

        <div className="space-y-2 text-sm mb-4">
          <Row label="Receipt No" value={payment.receiptNumber || '-'} mono />
          <Row label="Date" value={payment.paidAt ? new Date(payment.paidAt).toLocaleString('vi-VN') : '-'} />
          <Row label="Cashier" value={payment.cashierId?.slice(0, 8) + '…' || '-'} mono />
          <Row label="Appointment" value={payment.appointmentId.slice(0, 8) + '…'} mono />
        </div>

        <div className="border-t border-dashed border-gray-300 pt-4 space-y-2 text-sm mb-4">
          <Row label="Amount Due" value={formatVND(payment.amount)} />
          <Row label="Received" value={formatVND(payment.amountReceived ?? 0)} />
          <div className="border-t pt-2">
            <Row label="Change" value={formatVND(payment.changeReturned ?? 0)} bold />
          </div>
        </div>

        <div className="text-center text-xs text-gray-500 pt-4 border-t border-dashed border-gray-300 mb-4">
          Thank you for your payment.
        </div>

        <div className="flex gap-2 print:hidden">
          <button onClick={onClose} className="flex-1 px-4 py-2 border rounded text-sm hover:bg-gray-50">
            Close
          </button>
          <button
            onClick={() => window.print()}
            className="flex-1 px-4 py-2 bg-emerald-700 text-white rounded text-sm font-medium hover:bg-emerald-800 flex items-center justify-center gap-2"
          >
            <PrinterOutlined /> Print
          </button>
        </div>
      </div>
    </div>
  );
}

function Row({ label, value, mono, bold }: {
  label: string; value: string; mono?: boolean; bold?: boolean;
}) {
  return (
    <div className="flex justify-between">
      <span className="text-gray-500">{label}</span>
      <span className={`${mono ? 'font-mono text-xs' : ''} ${bold ? 'font-bold text-base' : ''}`}>
        {value}
      </span>
    </div>
  );
}

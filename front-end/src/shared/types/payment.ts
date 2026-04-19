export interface Payment {
  id: string;
  appointmentId: string;
  patientId: string;
  amount: number;
  currency: string;
  method: 'Cash' | 'Card' | 'Insurance' | 'BankTransfer';
  status: 'Pending' | 'Processing' | 'Completed' | 'Failed' | 'Refunded';
  transactionId?: string;
  description?: string;
  createdAt: string;
  paidAt?: string;
  // Cash-specific (populated when method=Cash and status=Completed)
  amountReceived?: number;
  changeReturned?: number;
  cashierId?: string;
  cashSessionId?: string;
  receiptNumber?: string;
}

export interface CashSession {
  id: string;
  cashierId: string;
  cashierName: string;
  counterId: string;
  openingBalance: number;
  expectedCash: number;
  actualCash?: number;
  variance?: number;
  status: 'Open' | 'Closed' | 'Reconciled';
  openedAt: string;
  closedAt?: string;
  notes?: string;
}

export interface CreatePaymentPayload {
  appointmentId: string;
  patientId: string;
  amount: number;
  method: 'Cash' | 'Card' | 'Insurance';
  description?: string;
}

export interface PaymentListResponse {
  data: Payment[];
  pagination: { total: number; page: number; pageSize: number };
}

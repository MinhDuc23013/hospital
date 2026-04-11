export interface Payment {
  id: string;
  appointmentId: string;
  patientId: string;
  amount: number;
  currency: string;
  method: 'Cash' | 'Card' | 'Insurance';
  status: 'Pending' | 'Completed' | 'Refunded';
  transactionId?: string;
  description?: string;
  createdAt: string;
  paidAt?: string;
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

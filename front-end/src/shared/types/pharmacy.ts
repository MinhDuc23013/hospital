export interface Drug {
  id: string;
  name: string;
  code: string;
  dosage?: string;
  quantity: number;
  price: number;
  lowStockThreshold: number;
  createdAt: string;
  updatedAt: string;
}

export interface Prescription {
  id: string;
  patientId: string;
  drugId: string;
  drugName: string;
  quantity: number;
  instructions: string;
  status: 'Pending' | 'Dispensed' | 'Cancelled';
  issuedAt: string;
}

export interface CreateDrugPayload {
  name: string;
  code: string;
  dosage?: string;
  quantity: number;
  price: number;
  lowStockThreshold?: number;
}

export interface UpdateDrugPayload {
  name?: string;
  dosage?: string;
  quantity?: number;
  price?: number;
  lowStockThreshold?: number;
}

export interface CreatePrescriptionPayload {
  patientId: string;
  drugId: string;
  drugName: string;
  quantity: number;
  instructions: string;
}

export interface DrugListResponse {
  data: Drug[];
  pagination: { total: number; page: number; pageSize: number };
}

// Provider (doctor) entity — used in appointment scheduling dropdown.

export interface Provider {
  id: string;
  name: string;
  specialty: string;
  email?: string;
  phoneNumber?: string;
}

// Patient entity — mirrors Patient Service PostgreSQL schema.
// Patient ID = Keycloak JWT sub (confirmed: no mapping needed).

export interface Patient {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  dateOfBirth: string;
  phoneNumber?: string;
  address?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

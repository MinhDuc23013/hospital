export interface KeycloakUser {
  id: string;
  email?: string;
  firstName?: string;
  lastName?: string;
  enabled: boolean;
  roles?: string[];
}

export interface CreateUserPayload {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  role: string;
}

export interface UpdateUserPayload {
  firstName?: string;
  lastName?: string;
  enabled?: boolean;
}

export interface UserListResponse {
  data: KeycloakUser[];
  pagination: { total: number; page: number; pageSize: number };
}

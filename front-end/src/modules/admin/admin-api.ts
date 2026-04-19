import api from '../../services/api';
import { ENDPOINTS } from '../../services/endpoints';
import type {
  KeycloakUser,
  UserListResponse,
  CreateUserPayload,
  UpdateUserPayload,
} from '../../shared/types/admin';

export const adminApi = {
  listUsers: (search?: string, page = 1, pageSize = 20) =>
    api
      .get<any>(ENDPOINTS.AUTH_USERS, {
        params: { search, page, pageSize },
      })
      .then(r => {
        // API may return either { data, pagination } or { data, page, pageSize }
        const body = r.data;
        const users: KeycloakUser[] = body.data ?? [];
        const pagination = body.pagination ?? {
          total: users.length,
          page: body.page ?? page,
          pageSize: body.pageSize ?? pageSize,
        };
        return { data: users, pagination } as UserListResponse;
      }),

  getUser: (userId: string) =>
    api.get<KeycloakUser>(`${ENDPOINTS.AUTH_USERS}/${userId}`).then(r => r.data),

  createUser: (payload: CreateUserPayload) =>
    api.post<KeycloakUser>(ENDPOINTS.AUTH_USERS, payload).then(r => r.data),

  updateUser: (userId: string, payload: UpdateUserPayload) =>
    api.put<KeycloakUser>(`${ENDPOINTS.AUTH_USERS}/${userId}`, payload).then(r => r.data),

  deleteUser: (userId: string) =>
    api.delete(`${ENDPOINTS.AUTH_USERS}/${userId}`).then(r => r.data),

  assignRole: (userId: string, role: string) =>
    api.post(`${ENDPOINTS.AUTH_USERS}/${userId}/roles`, { role }).then(r => r.data),

  removeRole: (userId: string, roleName: string) =>
    api.delete(`${ENDPOINTS.AUTH_USERS}/${userId}/roles/${roleName}`).then(r => r.data),

  resetPassword: (userId: string, newPassword: string) =>
    api
      .put(`${ENDPOINTS.AUTH_USERS}/${userId}/reset-password`, { newPassword })
      .then(r => r.data),
};

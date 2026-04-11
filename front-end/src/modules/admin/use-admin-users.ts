import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { adminApi } from './admin-api';
import type { CreateUserPayload, UpdateUserPayload } from '../../shared/types/admin';

const QUERY_KEY = 'admin-users';

export function useAdminUsers(search?: string, page = 1, pageSize = 20) {
  return useQuery({
    queryKey: [QUERY_KEY, search, page, pageSize],
    queryFn: () => adminApi.listUsers(search, page, pageSize),
  });
}

export function useCreateAdminUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: CreateUserPayload) => adminApi.createUser(payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}

export function useUpdateAdminUser(userId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: UpdateUserPayload) => adminApi.updateUser(userId, payload),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}

export function useDeleteAdminUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (userId: string) => adminApi.deleteUser(userId),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}

export function useResetPassword() {
  return useMutation({
    mutationFn: ({ userId, newPassword }: { userId: string; newPassword: string }) =>
      adminApi.resetPassword(userId, newPassword),
  });
}

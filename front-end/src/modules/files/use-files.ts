import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { fileApi } from './file-api';

const QUERY_KEY = 'files';

export function useFilesList(page = 1, pageSize = 50) {
  return useQuery({
    queryKey: [QUERY_KEY, page, pageSize],
    queryFn: () => fileApi.list(page, pageSize),
  });
}

export function useUploadFile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (file: File) => fileApi.upload(file),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}

export function useDeleteFile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => fileApi.remove(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}

export function useGetEditLink() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => fileApi.getEditLink(id),
    // First-time edit persists DriveFileId server-side — refetch so hasDriveLink flips
    // to true and the "Sync from Drive" button appears without a manual page reload.
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}

export function useSyncFromDrive() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => fileApi.syncFromDrive(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: [QUERY_KEY] }),
  });
}

export function useGoogleDriveStatus() {
  return useQuery({
    queryKey: ['google-drive-status'],
    queryFn: () => fileApi.getGoogleDriveStatus(),
  });
}

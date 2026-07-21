import api from '../../services/api';
import { ENDPOINTS } from '../../services/endpoints';
import type { FileMetadata, FileListResponse, DeleteFileResponse } from '../../shared/types/file';

export const fileApi = {
  upload: (file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    return api.post<FileMetadata>(ENDPOINTS.FILES, formData).then(r => r.data);
  },

  list: (page = 1, pageSize = 50) =>
    api
      .get<FileListResponse>(ENDPOINTS.FILES, { params: { page, pageSize } })
      .then(r => r.data),

  remove: (id: string) =>
    api.delete<DeleteFileResponse>(`${ENDPOINTS.FILES}/${id}`).then(r => r.data),

  getEditLink: (id: string) =>
    api.post<{ editUrl: string }>(`${ENDPOINTS.FILES}/${id}/edit-link`).then(r => r.data),

  syncFromDrive: (id: string) =>
    api.post<FileMetadata>(`${ENDPOINTS.FILES}/${id}/sync-from-drive`).then(r => r.data),

  getGoogleAuthUrl: () =>
    api.get<{ authUrl: string }>(`${ENDPOINTS.FILES}/google/auth-url`).then(r => r.data),

  submitGoogleCallback: (code: string) =>
    api
      .post<{ connected: boolean; email: string | null }>(`${ENDPOINTS.FILES}/google/callback`, { code })
      .then(r => r.data),

  getGoogleDriveStatus: () =>
    api.get<{ connected: boolean; email: string | null }>(`${ENDPOINTS.FILES}/google/status`).then(r => r.data),

  // Only call for text-previewable content types (text/plain, text/csv) — binary types
  // (xlsx/docx/pdf/images) would get corrupted by the text response type.
  getTextContent: (id: string) =>
    api.get<string>(`${ENDPOINTS.FILES}/${id}`, { responseType: 'text' }).then(r => r.data),

  // For binary preview (xlsx/docx), parsed client-side by xlsx/mammoth.
  getBinaryContent: (id: string) =>
    api.get<ArrayBuffer>(`${ENDPOINTS.FILES}/${id}`, { responseType: 'arraybuffer' }).then(r => r.data),
};

export interface FileMetadata {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  sha256: string | null;
  uploadedBy: string | null;
  uploadedAt: string;
  hasDriveLink: boolean;
}

export interface FileListResponse {
  data: FileMetadata[];
  pagination: { total: number; page: number; pageSize: number };
}

export interface DeleteFileResponse {
  success: boolean;
  message: string;
}

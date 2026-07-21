import { useRef, useState } from 'react';
import {
  useFilesList,
  useUploadFile,
  useDeleteFile,
  useGetEditLink,
  useSyncFromDrive,
  useGoogleDriveStatus,
} from './use-files';
import { fileApi } from './file-api';
import { FilePreviewModal } from './FilePreviewModal';
import { previewKindFor } from './preview-renderers';
import type { FileMetadata } from '../../shared/types/file';

function GoogleDriveStatusBanner() {
  const { data, isLoading } = useGoogleDriveStatus();
  const [connecting, setConnecting] = useState(false);

  if (isLoading || !data) return null;

  if (data.connected) {
    return (
      <p className="text-xs text-gray-500 mb-4">Google Drive connected as {data.email}.</p>
    );
  }

  const handleConnect = () => {
    setConnecting(true);
    fileApi
      .getGoogleAuthUrl()
      .then(res => {
        window.location.href = res.authUrl;
      })
      .catch(() => setConnecting(false));
  };

  return (
    <div className="p-3 mb-4 rounded text-sm border bg-yellow-50 border-yellow-200 text-yellow-700 flex items-center justify-between">
      <span>Google Drive is not connected.</span>
      <button
        onClick={handleConnect}
        disabled={connecting}
        className="px-3 py-1 bg-blue-600 text-white text-xs rounded hover:bg-blue-700 disabled:opacity-40"
      >
        {connecting ? 'Redirecting...' : 'Connect Google Drive'}
      </button>
    </div>
  );
}

function formatBytes(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

// Must stay in sync with backend's GoogleDriveEditService.ConvertibleContentTypes map.
const EDITABLE_CONTENT_TYPES = new Set([
  'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  'text/plain',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  'text/csv',
]);

function FileRow({
  file,
  onDelete,
  onEditError,
  onSyncSuccess,
  onSyncError,
  onPreview,
}: {
  file: FileMetadata;
  onDelete: (id: string) => void;
  onEditError: (text: string) => void;
  onSyncSuccess: (text: string) => void;
  onSyncError: (text: string) => void;
  onPreview: (file: FileMetadata) => void;
}) {
  const getEditLinkMutation = useGetEditLink();
  const syncFromDriveMutation = useSyncFromDrive();
  const isEditable = EDITABLE_CONTENT_TYPES.has(file.contentType);
  const isPreviewable = previewKindFor(file.contentType) !== 'unsupported';

  const handleEdit = () => {
    getEditLinkMutation.mutate(file.id, {
      onSuccess: data => {
        window.open(data.editUrl, '_blank', 'noopener,noreferrer');
      },
      onError: (err: unknown) => {
        const code = (err as { response?: { data?: { error?: { code?: string } } } })?.response?.data?.error?.code;
        onEditError(
          code === 'DRIVE_NOT_CONNECTED'
            ? 'Google Drive is not connected. Connect it above before editing files.'
            : 'Failed to open file for editing.'
        );
      },
    });
  };

  const handleSync = () => {
    syncFromDriveMutation.mutate(file.id, {
      onSuccess: () => onSyncSuccess(`Synced "${file.fileName}" from Google Drive.`),
      onError: () => onSyncError(`Failed to sync "${file.fileName}" from Google Drive.`),
    });
  };

  return (
    <tr className="border-t border-gray-100 hover:bg-gray-50">
      <td className="px-4 py-3 text-gray-800">
        {isPreviewable ? (
          <button onClick={() => onPreview(file)} className="text-blue-700 hover:underline text-left">
            {file.fileName}
          </button>
        ) : (
          file.fileName
        )}
      </td>
      <td className="px-4 py-3 text-gray-600 text-xs">{file.contentType}</td>
      <td className="px-4 py-3 text-gray-600 text-xs">{formatBytes(file.sizeBytes)}</td>
      <td className="px-4 py-3 text-gray-600 text-xs">{file.uploadedBy || '—'}</td>
      <td className="px-4 py-3 text-gray-600 text-xs">
        {new Date(file.uploadedAt).toLocaleString('vi-VN')}
      </td>
      <td className="px-4 py-3">
        <div className="flex gap-3 text-xs">
          <a
            href={`/api/files/${file.id}`}
            target="_blank"
            rel="noreferrer"
            className="text-blue-600 hover:underline"
          >
            Download
          </a>
          {isEditable && (
            <button
              onClick={handleEdit}
              disabled={getEditLinkMutation.isPending}
              className="text-blue-600 hover:underline disabled:opacity-40"
            >
              {getEditLinkMutation.isPending ? 'Opening...' : 'Edit'}
            </button>
          )}
          {file.hasDriveLink && (
            <button
              onClick={handleSync}
              disabled={syncFromDriveMutation.isPending}
              className="text-blue-600 hover:underline disabled:opacity-40"
            >
              {syncFromDriveMutation.isPending ? 'Syncing...' : 'Sync from Drive'}
            </button>
          )}
          <button
            onClick={() => onDelete(file.id)}
            className="text-red-600 hover:underline"
          >
            Delete
          </button>
        </div>
      </td>
    </tr>
  );
}

export default function FilesPage() {
  const [page, setPage] = useState(1);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [message, setMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);
  const [previewFile, setPreviewFile] = useState<FileMetadata | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const { data, isLoading, isError } = useFilesList(page, 50);
  const uploadMutation = useUploadFile();
  const deleteMutation = useDeleteFile();

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setSelectedFile(e.target.files?.[0] ?? null);
    setMessage(null);
  };

  const handleUpload = () => {
    if (!selectedFile) return;
    uploadMutation.mutate(selectedFile, {
      onSuccess: () => {
        setMessage({ type: 'success', text: `Uploaded "${selectedFile.name}" successfully.` });
        setSelectedFile(null);
        if (fileInputRef.current) fileInputRef.current.value = '';
      },
      onError: () => {
        setMessage({ type: 'error', text: 'Failed to upload file.' });
      },
    });
  };

  const handleDelete = (id: string) => {
    if (!window.confirm('Are you sure you want to delete this file?')) return;
    deleteMutation.mutate(id, {
      onError: () => setMessage({ type: 'error', text: 'Failed to delete file.' }),
    });
  };

  return (
    <div>
      <h1 className="text-2xl font-bold mb-6">Files</h1>

      <GoogleDriveStatusBanner />

      <div className="flex items-center gap-3 mb-4">
        <label className="px-4 py-2 bg-gray-100 border border-gray-300 text-sm rounded cursor-pointer hover:bg-gray-200">
          Choose File
          <input
            ref={fileInputRef}
            type="file"
            className="hidden"
            onChange={handleFileChange}
          />
        </label>
        <span className="text-sm text-gray-600">{selectedFile ? selectedFile.name : 'No file selected'}</span>
        <button
          onClick={handleUpload}
          disabled={!selectedFile || uploadMutation.isPending}
          className="px-4 py-2 bg-blue-600 text-white text-sm rounded hover:bg-blue-700 disabled:opacity-40"
        >
          {uploadMutation.isPending ? 'Uploading...' : 'Upload'}
        </button>
      </div>

      {message && (
        <div
          className={`p-3 mb-4 rounded text-sm border ${
            message.type === 'success'
              ? 'bg-green-50 border-green-200 text-green-600'
              : 'bg-red-50 border-red-200 text-red-600'
          }`}
        >
          {message.text}
        </div>
      )}

      {isLoading && <p className="text-gray-500">Loading...</p>}
      {isError && (
        <div className="p-3 bg-red-50 border border-red-200 rounded text-sm text-red-600">
          Failed to load files.
        </div>
      )}

      {data && (
        <>
          <div className="overflow-x-auto rounded border border-gray-200">
            <table className="w-full text-sm">
              <thead className="bg-gray-100 text-left">
                <tr>
                  <th className="px-4 py-3 font-medium">File Name</th>
                  <th className="px-4 py-3 font-medium">Content Type</th>
                  <th className="px-4 py-3 font-medium">Size</th>
                  <th className="px-4 py-3 font-medium">Uploaded By</th>
                  <th className="px-4 py-3 font-medium">Uploaded At</th>
                  <th className="px-4 py-3 font-medium"></th>
                </tr>
              </thead>
              <tbody>
                {data.data.map(f => (
                  <FileRow
                    key={f.id}
                    file={f}
                    onDelete={handleDelete}
                    onEditError={text => setMessage({ type: 'error', text })}
                    onSyncSuccess={text => setMessage({ type: 'success', text })}
                    onSyncError={text => setMessage({ type: 'error', text })}
                    onPreview={setPreviewFile}
                  />
                ))}
                {data.data.length === 0 && (
                  <tr>
                    <td colSpan={6} className="px-4 py-6 text-center text-gray-400">
                      No files uploaded yet.
                    </td>
                  </tr>
                )}
              </tbody>
            </table>
          </div>

          <div className="flex items-center justify-between mt-4 text-sm text-gray-600">
            <span>Total: {data.pagination.total} files</span>
            <div className="flex gap-2">
              <button
                onClick={() => setPage(p => Math.max(1, p - 1))}
                disabled={page === 1}
                className="px-3 py-1 border rounded disabled:opacity-40 hover:bg-gray-100"
              >
                Prev
              </button>
              <span className="px-3 py-1">Page {page}</span>
              <button
                onClick={() => setPage(p => p + 1)}
                disabled={page * 50 >= data.pagination.total}
                className="px-3 py-1 border rounded disabled:opacity-40 hover:bg-gray-100"
              >
                Next
              </button>
            </div>
          </div>
        </>
      )}

      {previewFile && (
        <FilePreviewModal
          fileId={previewFile.id}
          fileName={previewFile.fileName}
          contentType={previewFile.contentType}
          onClose={() => setPreviewFile(null)}
        />
      )}
    </div>
  );
}

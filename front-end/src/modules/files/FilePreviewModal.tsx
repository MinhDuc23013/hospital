import { useEffect, useState } from 'react';
import { fileApi } from './file-api';
import {
  previewKindFor,
  CsvPreview,
  TextPreview,
  XlsxPreview,
  DocxPreview,
} from './preview-renderers';

export function FilePreviewModal({
  fileId,
  fileName,
  contentType,
  onClose,
}: {
  fileId: string;
  fileName: string;
  contentType: string;
  onClose: () => void;
}) {
  const kind = previewKindFor(contentType);
  const [textContent, setTextContent] = useState<string | null>(null);
  const [binaryContent, setBinaryContent] = useState<ArrayBuffer | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    if (kind === 'csv' || kind === 'text') {
      fileApi.getTextContent(fileId).then(setTextContent).catch(() => setError(true));
    } else if (kind === 'xlsx' || kind === 'docx') {
      fileApi.getBinaryContent(fileId).then(setBinaryContent).catch(() => setError(true));
    }
  }, [fileId, kind]);

  return (
    <div
      className="fixed inset-0 bg-black/40 flex items-center justify-center z-50 p-4"
      onClick={onClose}
    >
      <div
        className="bg-white rounded-lg shadow-lg max-w-2xl w-full p-5"
        onClick={e => e.stopPropagation()}
      >
        <div className="flex items-center justify-between mb-4">
          <h2 className="font-semibold text-gray-800 truncate">{fileName}</h2>
          <button onClick={onClose} className="text-gray-400 hover:text-gray-600 text-xl leading-none">
            &times;
          </button>
        </div>

        {error && <p className="text-sm text-red-600">Failed to load file content.</p>}

        {!error && kind === 'csv' && (textContent === null ? <Loading /> : <CsvPreview text={textContent} />)}
        {!error && kind === 'text' && (textContent === null ? <Loading /> : <TextPreview text={textContent} />)}
        {!error && kind === 'xlsx' && (binaryContent === null ? <Loading /> : <XlsxPreview data={binaryContent} />)}
        {!error && kind === 'docx' && (binaryContent === null ? <Loading /> : <DocxPreview data={binaryContent} />)}
        {!error && kind === 'unsupported' && (
          <p className="text-sm text-gray-500">Preview not available for this file type.</p>
        )}
      </div>
    </div>
  );
}

function Loading() {
  return <p className="text-sm text-gray-500">Loading...</p>;
}

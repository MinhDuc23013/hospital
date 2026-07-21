import { useEffect, useState } from 'react';
import * as XLSX from 'xlsx';
import mammoth from 'mammoth';
import DOMPurify from 'dompurify';

export type PreviewKind = 'csv' | 'text' | 'xlsx' | 'docx' | 'unsupported';

export function previewKindFor(contentType: string): PreviewKind {
  switch (contentType) {
    case 'text/csv': return 'csv';
    case 'text/plain': return 'text';
    case 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': return 'xlsx';
    case 'application/vnd.openxmlformats-officedocument.wordprocessingml.document': return 'docx';
    default: return 'unsupported';
  }
}

function RowsTable({ rows }: { rows: unknown[][] }) {
  return (
    <div className="overflow-auto max-h-96 border rounded">
      <table className="w-full text-xs">
        <tbody>
          {rows.map((row, i) => (
            <tr key={i} className={i === 0 ? 'bg-gray-100 font-medium' : 'border-t'}>
              {row.map((cell, j) => (
                <td key={j} className="px-3 py-1.5">{String(cell ?? '')}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

export function CsvPreview({ text }: { text: string }) {
  const rows = text.trim().split('\n').map(line => line.split(','));
  return <RowsTable rows={rows} />;
}

export function TextPreview({ text }: { text: string }) {
  return (
    <pre className="text-xs whitespace-pre-wrap max-h-96 overflow-auto bg-gray-50 p-3 rounded border">
      {text}
    </pre>
  );
}

export function XlsxPreview({ data }: { data: ArrayBuffer }) {
  const workbook = XLSX.read(data, { type: 'array' });
  const sheet = workbook.Sheets[workbook.SheetNames[0]];
  const rows = XLSX.utils.sheet_to_json<unknown[]>(sheet, { header: 1 });
  return <RowsTable rows={rows} />;
}

export function DocxPreview({ data }: { data: ArrayBuffer }) {
  const [html, setHtml] = useState<string | null>(null);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    mammoth.convertToHtml({ arrayBuffer: data })
      .then(result => setHtml(DOMPurify.sanitize(result.value)))
      .catch(() => setFailed(true));
  }, [data]);

  if (failed) return <p className="text-sm text-red-600">Failed to render document.</p>;
  if (html === null) return <p className="text-sm text-gray-500">Rendering...</p>;

  return (
    <div
      className="text-sm max-h-96 overflow-auto border rounded p-4 space-y-2 [&_table]:border [&_td]:border [&_td]:px-2 [&_strong]:font-semibold"
      dangerouslySetInnerHTML={{ __html: html }}
    />
  );
}

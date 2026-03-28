// Read-only detail view for a single medical record.
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { formatDate } from "@/lib/utils/date-utils";
import type { MedicalRecord } from "@/lib/types";

interface RecordDetailProps {
  record: MedicalRecord;
}

export function RecordDetail({ record }: RecordDetailProps) {
  return (
    <div className="space-y-4">
      <Card>
        <CardHeader>
          <CardTitle className="text-base">Record Information</CardTitle>
        </CardHeader>
        <CardContent className="space-y-3 text-sm">
          <div>
            <span className="font-medium text-muted-foreground">Patient ID</span>
            <p className="mt-0.5">{record.patientId}</p>
          </div>
          <div>
            <span className="font-medium text-muted-foreground">Diagnosis</span>
            <p className="mt-0.5">{record.diagnosis.join(", ") || "—"}</p>
          </div>
          {record.findings && (
            <div>
              <span className="font-medium text-muted-foreground">Notes / Findings</span>
              <p className="mt-0.5 whitespace-pre-wrap">{record.findings}</p>
            </div>
          )}
          <div>
            <span className="font-medium text-muted-foreground">Created</span>
            <p className="mt-0.5">{formatDate(record.createdAt)}</p>
          </div>
        </CardContent>
      </Card>

      {record.labResults.length > 0 && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Lab Results</CardTitle>
          </CardHeader>
          <CardContent>
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-muted-foreground">
                  <th className="pb-2 text-left font-medium">Test</th>
                  <th className="pb-2 text-left font-medium">Result</th>
                  <th className="pb-2 text-left font-medium">Normal Range</th>
                </tr>
              </thead>
              <tbody>
                {record.labResults.map((lab, i) => (
                  <tr key={i} className="border-b last:border-0">
                    <td className="py-2 pr-4">{lab.testName}</td>
                    <td className="py-2 pr-4">{lab.result}</td>
                    <td className="py-2 text-muted-foreground">{lab.normalRange}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardContent>
        </Card>
      )}
    </div>
  );
}
